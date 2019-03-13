using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlToSql.Fluent;
using SqlToSql.Test.Nominas;

namespace SqlToSql.Test
{
    [TestClass]
    public class NominaTest
    {
        [TestMethod]
        public void RecalculoView()
        {
            //1.-
            //Obtiene los datos necesarios de las nominas, reg pat, ISR y subsidio:
            var q1 = Sql
                .From<NominaView>()
                .Inner().Join(new SqlTable<NominaTrabajador>()).On(x => x.Item2.IdRegistro == x.Item1.IdNominaTrabajador)
                .Inner().Join(new SqlTable<RegistroPatronal>()).On(x => x.Item3.IdRegistro == x.Item2.IdRegistroPatronal)
                .Inner().Join(new SqlTable<SalarioMinimo>()).On(x => x.Item4.IdRegistro == x.Item2.IdSalarioMinimo)
                .Inner().Join(new SqlTable<TablaIsr>()).On(x => x.Item5.IdRegistro == x.Item2.IdTablaIsr)
                .Alias(x => new
                {
                    v = x.Item1,
                    n = x.Item2,
                    r = x.Item3,
                    sm = x.Item4,
                    isr = x.Item5
                })
                .Select(x => new
                {
                    x.v.IdNominaTrabajador,
                    x.v.IdRegistroPatronal,
                    x.v.IdTrabajador,
                    x.v.PeriodoIni,
                    x.v.PeriodoFin,
                    Mes = Sql.DateTrunc(Sql.DateTruncField.Month, x.v.PeriodoFin),
                    x.v.IniBim,
                    x.v.DiasBimInfona,
                    x.v.PercGravado,
                    x.v.DiasVacPagados,
                    x.v.DiasVacDisfrutados,
                    x.n.DiasTrab,
                    DiasBaseIsr = x.n.DiasTrab + x.v.DiasVacDisfrutados,
                    SD = x.n.SalarioDiario,
                    SDI = x.n.SalarioDiarioIntegrado,
                    x.n.PrimaVacacional,
                    x.n.AplicaTablaIsr,
                    x.v.ISR,
                    x.v.IsrAntesSub,
                    x.v.SeguroVivInfona,
                    TipoCredInfona = x.n.TipoCredito,
                    ValorCredInfona = x.n.ValorCredito,
                    x.n.SubCausado,

                    //Salario minimo:
                    x.sm.SalarioMin,
                    x.sm.UMA,
                    x.sm.UMI,
                    x.sm.FactorDecreto,
                    x.sm.FactorIntegracion,
                    x.sm.PorcIMSS,
                    x.sm.PorcISRAdmin,

                    //ISR:
                    IdTablaIsr = x.isr.IdRegistro,
                    ElevacionTarifaMesIsr = x.r.ElevacionTarifa
                });

            //2.-
            //Obtiene los acumulados por mes(para el ISR) y bimestre(para el infonavit)
            //Obtiene el calculo del sueldo(CSueldo)
            //Calculo de las vacaciones

            var q2 = Sql
                .From(q1)
                .Window(w => new
                {
                    mesAct = w
                        .PartitionBy(x => x.IdRegistroPatronal).ThenBy(x => x.IdTrabajador).ThenBy(x => x.Mes)
                        .OrderBy(x => x.PeriodoFin)
                        .Rows().UnboundedPreceding().AndCurrentRow(),
                    mesActAnt = w
                        .PartitionBy(x => x.IdRegistroPatronal).ThenBy(x => x.IdTrabajador).ThenBy(x => x.Mes)
                        .OrderBy(x => x.PeriodoFin)
                        .Rows().UnboundedPreceding().AndPreceding(1),
                    bimActAnt =
                        w
                        .PartitionBy(x => x.IdRegistroPatronal).ThenBy(x => x.IdTrabajador).ThenBy(x => x.IniBim)
                        .OrderBy(x => x.PeriodoFin)
                        .Rows().UnboundedPreceding().AndPreceding(1)
                })
                .Select((x, w) => new
                {
                    x,
                    AcumMesDias = Sql.Over(Sql.Sum(x.DiasBaseIsr), w.mesAct),
                    AcumMesPercGravado = Sql.Over(Sql.Sum(x.PercGravado), w.mesAct),
                    AcumMesIsrAntesSub = Sql.Coalesce(Sql.Over(Sql.Sum(x.IsrAntesSub), w.mesActAnt), 0),
                    AcumMesSubCausado = Sql.Coalesce(Sql.Over(Sql.Sum(x.SubCausado), w.mesActAnt), 0),

                    AcumBimSeguroViv = Sql.Coalesce(Sql.Over(Sql.Sum(x.SeguroVivInfona), w.bimActAnt), 0),

                    CSueldo = x.SD * (x.DiasTrab - x.DiasVacDisfrutados),
                    Sdi25 = Sql.Least(x.SDI, 25 * x.UMA),
                    CVacaciones = x.SD * (x.DiasVacDisfrutados + x.DiasVacPagados)
                })
                ;

            //********************************************************
            //3
            //Obtiene la base mensual para el ISR
            //Obtiene la retención del IMSS (CRetImss)
            //Obtiene la deducción del abono al infonavit (CInfonavit)
            //Obtiene la deducción bimestral del seguro de vivienda infonavit (CSeguroViv)
            //Calculo de la prima vacacional
            //****************************	
            var q3 = Sql
                .From(q2)
                .Select(x => new
                {
                    x,
                    BaseMensualIsr = Sql.Round(x.AcumMesPercGravado / x.AcumMesDias * x.x.ElevacionTarifaMesIsr, 2),
                    CRetImss = Sql.Round(
                            //Si el salario diario es el minimo, no hay retención
                            x.x.SD == x.x.SalarioMin ? 0 :
                            //Si el SDI es menor a 3 umas
                            x.x.SDI < (x.x.UMA * 3) ? (x.x.SDI * x.x.PorcIMSS * x.x.DiasTrab) :

                            //Si el SDI supera o es igual a 3 umas:
                            (x.x.DiasTrab * ((x.Sdi25 * x.x.PorcIMSS) + (Sql.Greatest(x.Sdi25 - (3 * x.x.UMA), 0) * 0.004M)))
                        , 2),
                    CInfonavit = Sql.Round(
                            x.x.TipoCredInfona == TipoCreditoInfona.Porcentaje ? (x.x.SDI * x.x.ValorCredInfona * x.x.DiasTrab) :
                            x.x.TipoCredInfona == TipoCreditoInfona.CuotaFija ? (x.x.ValorCredInfona * 2 / x.x.DiasBimInfona * x.x.DiasTrab) :
                            x.x.TipoCredInfona == TipoCreditoInfona.VecesSalarioMinimo ? (x.x.ValorCredInfona * x.x.UMI * 2 / x.x.DiasBimInfona * x.x.DiasTrab) :
                            0M
                        , 2),
                    CSeguroViv = Sql.Round((x.x.TipoCredInfona != null && (x.AcumBimSeguroViv == 0)) ? 15.0M : 0.0M, 2),
                    CPrimaVac = Sql.Round(x.CVacaciones * x.x.PrimaVacacional, 2)
                });


            //4
            //Obtiene los valores que corresponden a la base mensual del ISR de las tablas de ISR y del subsidio al empleo

            var q4 = Sql
                .From(q3)
                .Left().Lateral(q =>
                    Sql
                    .From<TarifaISR>()
                    .Select(x => x)
                    .Where(x => x.IdTablaIsr == q.x.x.IdTablaIsr && x.LimiteInf <= q.BaseMensualIsr)
                    .OrderBy(x => x.LimiteInf, OrderByOrder.Desc)
                    .Limit(1)
                ).On(x => true)
              ;


            var actual = q3.ToSql();


        }
    }
}
