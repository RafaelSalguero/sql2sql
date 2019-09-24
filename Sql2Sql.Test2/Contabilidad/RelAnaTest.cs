using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sql2Sql.Fluent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sql2Sql.Test.Contabilidad
{
    [TestClass]
    public class RelAnaTest
    {
        public enum TipoCuenta
        {
            Mayor = 0,
            Acumulativa = 1,
            Detalle = 2
        }

        public interface ICuentaCargosAbonos
        {
            Guid? IdCuentaDet { get; }
            Guid? IdCuentaAcum { get; }
            decimal CargoAnt { get; }
            decimal AbonoAnt { get; }
            decimal CargoPer { get; }
            decimal AbonoPer { get; }
        }
        public class CuentasRaizDet
        {
            /// <summary>
            /// Al obtener las cuentas de detalle de cierto conjunto de cuentas, es la cuenta por la cual se incluyó esta cuenta de detalle
            /// </summary>
            public Guid IdRaiz { get; set; }
            /// <summary>
            /// En caso de que sea cuenta de detalle, es el id
            /// </summary>
            public Guid? IdCuentaDet { get; set; }
            /// <summary>
            /// En caso de que sea cuenta acumulativa, es el id
            /// </summary>
            public Guid? IdCuentaAcum { get; set; }


            /// <summary>
            /// Terminación de la cuenta
            /// </summary>
            public int Terminacion { get; set; }

            /// <summary>
            /// Nombre de la cuenta
            /// </summary>
            public string Nombre { get; set; }

            /// <summary>
            /// Id de la cuenta padre de esta cuenta
            /// </summary>
            public Guid? IdCuentaPadre { get; set; }

            /// <summary>
            /// Tipo de la cuenta
            /// </summary>
            public TipoCuenta Tipo { get; set; }
        }

        public class CuentasRaizDetSaldo : CuentasRaizDet, ICuentaCargosAbonos
        {
            public decimal CargoAnt { get; set; }
            public decimal AbonoAnt { get; set; }

            public decimal CargoPer { get; set; }
            public decimal AbonoPer { get; set; }
        }

        public class SaldoFiltro
        {
            public DateTime FechaIni { get; set; }
            public DateTime FechaFin { get; set; }
        }

        public class CuentasFiltro
        {
            public Guid? IdCuenta { get; set; }
        }

        public class MovCargoAbono
        {
            public decimal CargoAnt { get; set; }
            public decimal AbonoAnt { get; set; }

            public decimal CargoPer { get; set; }
            public decimal AbonoPer { get; set; }
        }

        public class Mov
        {
            public decimal Cargo { get; }
            public decimal Abono { get; }
            public DateTime Fecha { get; }
        }

        /// <summary>
        /// Naturaleza de la cuenta
        /// </summary>
        public enum NaturalezaCuenta
        {
            /// <summary>
            /// Indica que el saldo de la cuenta sera: Abonos - Cargos
            /// </summary>
            Acreedora = 0,
            /// <summary>
            /// Indica que el saldo de la cuenta sera: Cargos - Abonos
            /// </summary>
            Deudora = 1
        }

        /// <summary>
        /// Tipo de cuenta
        /// </summary>
        public enum TipoCuentaContable
        {
            /// <summary>
            /// Cuenta de activo, clasificada de balance
            /// </summary>
            Activo = 0,

            /// <summary>
            /// Cuenta de pasivo, clasificada de balance 
            /// </summary>
            Pasivo = 1,

            /// <summary>
            /// Cuenta de capital, clasificada de balance
            /// </summary>
            Capital = 2,

            /// <summary>
            /// Cuenta de ingresos, clasificada de resultados
            /// </summary>
            Ingresos = 3,

            /// <summary>
            /// Cuenta de egresos, clasificada de resultados
            /// </summary>
            Egresos = 4,

            /// <summary>
            /// Cuenta de orden, clasificada de balance
            /// </summary>
            Orden = 5
        }

        public interface ICuenta
        {
            Guid? IdRegistro { get; }
            string Nombre { get; }

            Guid? IdCuentaMayor { get; }
            string Numero { get; }

        }

        public interface ICuentaMayor : ICuenta
        {
            NaturalezaCuenta Naturaleza { get; }
            TipoCuentaContable TipoCuenta { get; }
        }

        /// <summary>
        /// Información del saldo de las cuentas, incluída información extra de las cuentas
        /// </summary>
        public class CuentaSaldoDto
        {
            public Guid IdCuenta { get; }
            public Guid? IdCuentaDet { get; set; }
            public Guid? IdCuentaAcum { get; set; }
            public Guid? IdCuentaMayor { get; set; }


            public decimal CargoAnt { get; set; }
            public decimal AbonoAnt { get; set; }

            public decimal CargoPer { get; set; }
            public decimal AbonoPer { get; set; }

            public decimal CargoAct { get; set; }
            public decimal AbonoAct { get; set;  }

            public decimal SaldoAnt { get; set; }
            public decimal SaldoPer { get; set; }
            public decimal SaldoAct { get; set; }

            public string Nombre { get; set; }
            public string Numero { get; set; }

            public NaturalezaCuenta Naturaleza { get; set; }
            public TipoCuentaContable Tipo { get; set; }
        }

        /// <summary>
        /// Dado un query que obtiene los cargos y abonos de un conjunto de cuentas, devuelve un DTO con los saldos de la cuenta, según su naturaleza y otros datos extras
        /// </summary>
        /// <param name="cargosAbo"></param>
        /// <returns></returns>
        static ISqlSelect<CuentaSaldoDto> QuerySaldoDto(ISqlSelect<ICuentaCargosAbonos> cargosAbo)
        {
            var mayor =
                //Obtener el IdCuentaMayor y el Numero de la cuenta, ya sea la de detalle o la acumulativa:
                Sql
                .From(cargosAbo)
                .Left().JoinTable<ICuenta>("CuentaDetalle").OnTuple(x => x.Item2.IdRegistro == x.Item1.IdCuentaDet)
                .Left().JoinTable<ICuenta>("CuentaAcumulativa").On(x => x.Item3.IdRegistro == x.Item1.IdCuentaAcum)
                .Alias(x => new
                {
                    carAbo = x.Item1,
                    det = x.Item2,
                    acum = x.Item3
                })
                .Select(x => new
                {
                    x.carAbo.CargoAnt,
                    x.carAbo.AbonoAnt,
                    x.carAbo.CargoPer,
                    x.carAbo.AbonoPer,
                    x.carAbo.IdCuentaDet,
                    x.carAbo.IdCuentaAcum,

                    IdCuenta = Sql.Coalesce(x.carAbo.IdCuentaDet, x.carAbo.IdCuentaAcum),
                    IdCuentaMayor = Sql.Coalesce(x.det.IdCuentaMayor, x.acum.IdCuentaMayor),
                    Numero = Sql.Coalesce(x.det.Numero, x.acum.Numero),
                    Nombre = Sql.Coalesce(x.det.Nombre, x.acum.Nombre)
                });

            var dto = Sql
                .From(mayor)
                .Inner().JoinTable<ICuentaMayor>("CuentaAcumulativa").OnTuple(x => x.Item2.IdRegistro == x.Item1.IdCuentaMayor)
                .Alias(x => new
                {
                    cuenta = x.Item1,
                    mayor = x.Item2
                })
                .Select(x => new CuentaSaldoDto
                {
                    Numero = x.cuenta.Numero,

                    CargoAnt = x.cuenta.CargoAnt,
                    AbonoAnt = x.cuenta.AbonoAnt,

                    CargoPer = x.cuenta.CargoPer,
                    AbonoPer = x.cuenta.AbonoPer,

                    CargoAct = x.cuenta.CargoAnt + x.cuenta.CargoPer,
                    AbonoAct = x.cuenta.AbonoAnt + x.cuenta.AbonoPer,

                    SaldoAnt = (x.mayor.Naturaleza == NaturalezaCuenta.Acreedora ? 1 : -1) * (x.cuenta.AbonoAnt - x.cuenta.CargoAnt),
                    SaldoPer = (x.mayor.Naturaleza == NaturalezaCuenta.Acreedora ? 1 : -1) * (x.cuenta.AbonoPer - x.cuenta.CargoPer),
                    SaldoAct = (x.mayor.Naturaleza == NaturalezaCuenta.Acreedora ? 1 : -1) * ((x.cuenta.AbonoAnt + x.cuenta.AbonoPer) - (x.cuenta.CargoAnt + x.cuenta.CargoPer)),

                    IdCuentaDet = x.cuenta.IdCuentaDet,
                    IdCuentaAcum = x.cuenta.IdCuentaAcum,
                    IdCuentaMayor = x.cuenta.IdCuentaMayor,

                    Nombre = x.cuenta.Nombre,

                    Naturaleza = x.mayor.Naturaleza,
                    Tipo = x.mayor.TipoCuenta
                });

            return dto;
        }

        /// <summary>
        /// Dado un query que obtiene los saldos de un conjunto de cuentas de detalle, acumula todos los niveles de estos saldos hasta llegar a las cuentas mayores,
        /// esto devuelve los saldos por cada uno de los detalles y los saldos de cada una de las cuentas acumulativas incluidas. El resultado es muy similar a la relación analítica
        /// </summary>
        static ISqlSelect<CuentasRaizDetSaldo> QueryAcumularSaldosDetalle(ISqlSelect<CuentasRaizDetSaldo> saldosDetalle)
        {
            var q = Sql
                .With(saldosDetalle)
                //Obtener los acumulados de todos los niveles hacia arriba del detale
                .WithRecursive(
                    detalle => Sql.From(detalle).Select(x => x)
                ).UnionAll(
                //Buscamos todos los padres directos de las cuentas:
                (w, rec) => Sql.RawSubquery<CuentasRaizDetSaldo>(@"
                    SELECT 
	                    d.""IdRaiz"", null::uuid AS ""IdCuentaDet"", d.""IdCuentaPadre"" AS ""IdCuentaAcum"", c.""IdCuentaPadre"", c.""Terminacion"", c.""Nombre"", 
	                    d.""CargoAnt"", d.""AbonoAnt"", d.""CargoPer"", d.""AbonoPer""
                    FROM rec d
                    JOIN ""CuentaAcumulativa"" c ON c.""IdRegistro"" = d.""IdCuentaPadre""
                    "))
                .Map((det, rec) => new
                {
                    det,
                    rec
                })
                .Query(w =>
                      //Sumar por cuenta:
                      Sql.RawSubquery<CuentasRaizDetSaldo>(@"
                    SELECT 
	
	                    d.""IdRaiz"", d.""IdCuentaDet"", d.""IdCuentaAcum"", d.""IdCuentaPadre"", d.""Terminacion"", d.""Nombre"",
	                    sum(d.""CargoAnt"") AS ""CargoAnt"", sum(d.""AbonoAnt"") AS ""AbonoAnt"", sum(d.""CargoPer"") AS ""CargoPer"", sum(d.""AbonoPer"") AS ""AbonoPer""
                    FROM rec d
                    GROUP BY d.""IdRaiz"", d.""IdCuentaDet"", d.""IdCuentaAcum"" , d.""IdCuentaPadre"", d.""Terminacion"", d.""Nombre""
                    ")
                );
            return q;
        }

        /// <summary>
        /// Query que devuelve los saldos de un conjunto de cuentas detalle
        /// </summary>
        /// <param name="detalle">Subquery que tiene un renglon por cada cuenta de detalle de interés</param>
        static ISqlSelect<CuentasRaizDetSaldo> QuerySaldosDetalle(ISqlSelect<CuentasRaizDet> detalle, SaldoFiltro filtro)
        {
            var q = Sql
                .From(detalle)
                .Left().Lateral(det =>
                Sql.From(Sql.RawSubquery<Mov>(@"
	SELECT 
		CASE mov.""TipoMovimiento"" WHEN 0 THEN mov.""Importe"" ELSE 0 END AS ""Cargo"",
		CASE mov.""TipoMovimiento"" WHEN 1 THEN mov.""Importe"" ELSE 0 END AS ""Abono"",
		pol.""Fecha""
	FROM ""Movimiento"" mov 
	JOIN ""Poliza"" pol ON pol.""IdRegistro"" = mov.""IdPoliza""
	WHERE 
		mov.""IdCuentaDetalle"" = ""det"".""IdCuentaDet"" AND
		pol.""Aplicada"" AND NOT pol.""Borrada""
"))
                .Select(x => new MovCargoAbono
                {
                    CargoPer = Sql.Coalesce(Sql.Filter(Sql.Sum(x.Cargo), Sql.Between(x.Fecha, filtro.FechaIni, filtro.FechaFin)), 0),
                    AbonoPer = Sql.Coalesce(Sql.Filter(Sql.Sum(x.Abono), Sql.Between(x.Fecha, filtro.FechaIni, filtro.FechaFin)), 0),

                    CargoAnt = Sql.Coalesce(Sql.Filter(Sql.Sum(x.Cargo), x.Fecha < filtro.FechaIni), 0),
                    AbonoAnt = Sql.Coalesce(Sql.Filter(Sql.Sum(x.Abono), x.Fecha < filtro.FechaIni), 0),
                }))
             .OnMap((a, b) => new
             {
                 det = a,
                 mov = b
             }, x => true)
             .Select(x => new CuentasRaizDetSaldo
             {
                 IdRaiz = x.det.IdRaiz,
                 IdCuentaDet = x.det.IdCuentaDet,
                 IdCuentaAcum = x.det.IdCuentaAcum,
                 IdCuentaPadre = x.det.IdCuentaPadre,
                 Terminacion = x.det.Terminacion,
                 Nombre = x.det.Nombre,

                 CargoAnt = x.mov.CargoAnt,
                 AbonoAnt = x.mov.AbonoAnt,

                 CargoPer = x.mov.CargoPer,
                 AbonoPer = x.mov.AbonoPer
             });

            return q;
        }

        /// <summary>
        /// Query que devuelve todas las cuentas de detalle relacionadas con cierto conjunto de cuentas
        /// </summary>
        /// <returns></returns>
        static ISqlSelect<CuentasRaizDet> QueryCuentasDetalle(CuentasFiltro filtro)
        {
            var q = Sql.WithRecursive(
                Sql
                //Cuentas de las que nos interesa la relación analítica:
                .From(Sql.RawSubquery<CuentasRaizDet>(
                    @"
            SELECT
			    ""IdRegistro"" AS ""IdRaiz"",
			    null::uuid AS ""IdCuentaDet"",
			    ""IdRegistro"" AS ""IdCuentaAcum"",
			    ""Terminacion"",
			    ""Nombre"",
			    ""IdCuentaPadre"",
			    CASE WHEN ""IdCuentaPadre"" IS NULL THEN 0 ELSE 1 END AS ""Tipo""
            FROM ""CuentaAcumulativa""
                
            UNION ALL
                
            SELECT
			    ""IdRegistro"" AS ""IdRaiz"",
			    ""IdRegistro"" AS ""IdCuentaDet"",
			    null::uuid AS ""IdCuentaAcum"",
			    ""Terminacion"",
			    ""Nombre"",
			    ""IdCuentaPadre"", 
			    2 AS ""Tipo""
            FROM ""CuentaDetalle""
"
                    ))
                .Select(x => x)
                //Filtros por cuenta:
                .Where(x => filtro.IdCuenta == null || x.IdCuentaAcum == filtro.IdCuenta)
            ).UnionAll((w, cuentas) =>
                //Obtener todas las subcuentas hijas de esas cuentas de interes, note que aquí estan revueltas las acumulativas como las de detalle
                Sql.RawSubquery<CuentasRaizDet>(@"
            SELECT cuentas.""IdRaiz"", ac.""IdCuentaDet"", ac.""IdCuentaAcum"", ac.""Terminacion"", ac.""Nombre"", ac.""IdCuentaPadre"", ac.""Tipo"" 
            FROM
            (
		    SELECT 
			    null::uuid AS ""IdCuentaDet"",
			    ""IdRegistro"" AS ""IdCuentaAcum"", 
			    ""Terminacion"", 
			    ""Nombre"", 
			    ""IdCuentaPadre"", 
			    1 AS ""Tipo"" 
		    FROM ""CuentaAcumulativa""

		    UNION ALL
		    
		    SELECT 
			    ""IdRegistro"" AS ""IdCuentaDet"",
			    null::uuid AS ""IdCuentaAcum"", 
			    ""Terminacion"", 
			    ""Nombre"", 
			    ""IdCuentaPadre"", 
			    2 AS ""Tipo"" 
		    FROM ""CuentaDetalle""
            )  ac, cuentas 
            WHERE ac.""IdCuentaPadre"" = cuentas.""IdCuentaAcum""
"))
            .Map((w, b) => b)
            .Query(cuentas =>
                Sql.From(cuentas)
                .Select(x => x)
                //Sólo nos interesan las de detalle:
                .Where(x => x.Tipo == TipoCuenta.Detalle)
                //El orden es sólo por conveniencia para depurar:
                .OrderBy(x => x.Terminacion)
            );

            return q;
        }


        [TestMethod]
        public void RelacionAnalitica()
        {
            var cuentasFiltro = new CuentasFiltro
            {
                IdCuenta = new Guid("02bcd575-75ec-48bb-af43-c517fe65af4f")
            };

            var saldosFiltro = new SaldoFiltro
            {
                FechaIni = new DateTime(2018, 12, 1),
                FechaFin = new DateTime(2018, 12, 31)
            };

            var cuentas = QueryCuentasDetalle(cuentasFiltro);
            var saldosDet = QuerySaldosDetalle(cuentas, saldosFiltro);
            var acumulados = QueryAcumularSaldosDetalle(saldosDet);

            var saldos = QuerySaldoDto(acumulados);
            var ret = saldos.ToSql(SqlText.ParamMode.Substitute);
        }
    }
}
