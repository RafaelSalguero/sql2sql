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
            //Obtiene los datos necesarios de las nominas, reg pat, ISR y subsidio:
            var q1 = Sql
                .From<NominaView>()
                .Inner().Join(new SqlTable<NominaTrabajador>()).On1(x => x.Item2.IdRegistro == x.Item1.IdNominaTrabajador)
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
                    AcumMesDias = Sql.Over(Sql.Sum(x.DiasBaseIsr), w.mesAct)
                })
                ;

            var actual = q2.ToSql();
        }
    }
}
