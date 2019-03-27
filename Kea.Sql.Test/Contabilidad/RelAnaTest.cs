using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public class CuentasRelAn
        {
            public Guid IdRaiz { get; set; }
            public Guid IdRegistro { get; set; }
            public int Terminacion { get; set; }
            public string Nombre { get; set; }
            public Guid IdCuentaPadre { get; set; }
            public TipoCuenta Tipo { get; set; }
        }


        [TestMethod]
        public void RelacionAnalitica()
        {
           var q =  Sql.WithRecursive(
                Sql
                //Cuentas de las que nos interesa la relación analítica:
                .From(Sql.RawSubquery<CuentasRelAn>(
                    @"
SELECT 
	""IdRegistro"" AS ""IdRaiz"", 
	""IdRegistro"", 
	""Terminacion"", 
	""Nombre"", 
	""IdCuentaPadre"", 
	CASE WHEN ""IdCuentaPadre"" IS NULL THEN 0 ELSE 1 END AS ""Tipo"" 
	FROM ""CuentaAcumulativa"" 
UNION ALL 
SELECT 
	""IdRegistro"" AS ""IdRaiz"",
	""IdRegistro"", 
	""Terminacion"", 
	""Nombre"", 
	""IdCuentaPadre"", 
	2 AS ""Tipo"" 
	FROM ""CuentaDetalle"" 
"
                    ))
                .Select(x => x)
                //Filtros por cuenta:
                .Where(x => x.IdRegistro == Sql.Raw<Guid>("'02bcd575-75ec-48bb-af43-c517fe65af4f'"))
            ).UnionAll((w, cuentas) =>
                //Obtener todas las subcuentas hijas de esas cuentas de interes, note que aquí estan revueltas las acumulativas como las de detalle
                Sql.RawSubquery<CuentasRelAn>(@"
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
            );

            var ret = q.ToSql();
        }
    }
}
