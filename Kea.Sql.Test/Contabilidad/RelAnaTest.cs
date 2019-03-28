﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeaSql.Fluent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KeaSql.Test.Contabilidad
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
        public class CuentasRaizDet
        {
            public Guid IdRaiz { get; set; }
            public Guid IdCuenta { get; set; }
            public int Terminacion { get; set; }
            public string Nombre { get; set; }
            public Guid IdCuentaPadre { get; set; }
            public TipoCuenta Tipo { get; set; }
        }

        public class CuentasRaizDetSaldo : CuentasRaizDet
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
        /// Query que devuelve el saldo de un conjunto de cuentas detalle
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
		mov.""IdCuentaDetalle"" = ""det"".""IdRegistro"" AND
		pol.""Aplicada"" AND NOT pol.""Borrada""
"))
                .Select(x => new MovCargoAbono
                {
                    CargoPer = Sql.Coalesce(Sql.Filter(Sql.Sum(x.Cargo), Sql.Between(x.Fecha, filtro.FechaIni, filtro.FechaFin)), 0),
                    AbonoPer = Sql.Coalesce(Sql.Filter(Sql.Sum(x.Abono), Sql.Between(x.Fecha, filtro.FechaIni, filtro.FechaFin)), 0),

                    CargoAnt = Sql.Coalesce(Sql.Filter(Sql.Sum(x.Cargo), x.Fecha < filtro.FechaIni), 0),
                    AbonoAnt = Sql.Coalesce(Sql.Filter(Sql.Sum(x.Abono), x.Fecha < filtro.FechaIni), 0),
                }))
             .OnMap((a,b) => new
             {
                 det = a,
                 mov = b
             },x => true)
             .Select(x => new CuentasRaizDetSaldo
             {
                  IdRaiz = x.det.IdRaiz,
                  IdCuenta = x.det.IdCuenta,
                  IdCuentaPadre = x.det.IdCuentaPadre,
                  Terminacion = x.det.Terminacion,
                  Nombre = x.det.Nombre,

                  CargoAnt = x.mov.CargoAnt,
                  AbonoAnt = x.mov.AbonoAnt,

                  CargoPer = x.mov.CargoPer,
                  AbonoPer = x.mov.AbonoPer
             })
                ;


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
	""IdRegistro"" AS ""IdCuenta"", 
	""Terminacion"", 
	""Nombre"", 
	""IdCuentaPadre"", 
	CASE WHEN ""IdCuentaPadre"" IS NULL THEN 0 ELSE 1 END AS ""Tipo"" 
	FROM ""CuentaAcumulativa"" 
UNION ALL 
SELECT 
	""IdRegistro"" AS ""IdRaiz"",
	""IdRegistro"" AS ""IdCuenta"", 
	""Terminacion"", 
	""Nombre"", 
	""IdCuentaPadre"", 
	2 AS ""Tipo"" 
	FROM ""CuentaDetalle"" 
"
                    ))
                .Select(x => x)
                //Filtros por cuenta:
                .Where(x => filtro.IdCuenta == null || x.IdCuenta == filtro.IdCuenta)
            ).UnionAll((w, cuentas) =>
                //Obtener todas las subcuentas hijas de esas cuentas de interes, note que aquí estan revueltas las acumulativas como las de detalle
                Sql.RawSubquery<CuentasRaizDet>(@"
	SELECT cuentas.""IdRaiz"", ac.""IdRegistro"", ac.""Terminacion"", ac.""Nombre"", ac.""IdCuentaPadre"", ac.""Tipo"" FROM 
	(
		SELECT ""IdRegistro"", ""Terminacion"", ""Nombre"", ""IdCuentaPadre"", 1 AS ""Tipo"" FROM ""CuentaAcumulativa"" 
		UNION ALL 
		SELECT ""IdRegistro"", ""Terminacion"", ""Nombre"", ""IdCuentaPadre"", 2 AS ""Tipo"" FROM ""CuentaDetalle"" 
	)  ac, cuentas WHERE ac.""IdCuentaPadre"" = cuentas.""IdRegistro""
"
            ))
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
            var filtro = new CuentasFiltro
            {
                IdCuenta = new Guid("02bcd575-75ec-48bb-af43-c517fe65af4f")
            };
            var q = QueryCuentasDetalle(filtro);

            var ret = q.ToSql();
        }
    }
}
