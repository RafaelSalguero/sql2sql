using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeaSql;
using KeaSql.SqlText;
using KeaSql.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static KeaSql.ExprTree.ExprReplace;

namespace KeaSql.Test
{
    [TestClass]
    public class WithTest
    {
        [TestMethod]
        public void WithSyntax()
        {
            var with = Sql.With(
                 Sql.From<Cliente>()
                 .Select(x => x)
             ).With(cli =>
                 Sql
                 .From<Factura>()
                 .Inner().Join(cli).OnTuple(y => y.Item1.IdCliente == y.Item2.IdRegistro)
                 .Select(z => new
                 {
                     z.Item1.IdCliente,
                     z.Item2.Nombre,
                 })
             ).Map((a, b) => new
             {
                 cliente = a,
                 facturas = b
             })
             .WithRecursive(c =>
                 Sql
                 .From<ConceptoFactura>()
                 .Inner().Join(c.facturas).OnTuple(d => d.Item1.IdFactura == d.Item2.IdCliente)
                 .Select(e => e.Item1)
             ).UnionAll((w, conceptos) =>
                 Sql.From(conceptos)
                 .Select(f => f)
             ).Map((g, h) => new
             {
                 cli = g.cliente,
                 fact = g.facturas,
                 conc = h
             })
             .Query(w => 
                Sql.From(w.conc).Select(x => x)
             );

            var actual = with.ToSql().Sql;
            var expected = @"
WITH ""cli"" AS (
    SELECT 
        ""x"".*
    FROM ""Cliente"" ""x""
), ""fact"" AS (
    SELECT 
        ""Item1"".""IdCliente"" AS ""IdCliente"", 
        ""Item2"".""Nombre"" AS ""Nombre""
    FROM ""Factura"" ""Item1""
    JOIN ""cli"" ""Item2"" ON (""Item1"".""IdCliente"" = ""Item2"".""IdRegistro"")
), RECURSIVE ""conc"" AS (
    SELECT 
        ""Item1"".*
    FROM ""ConceptoFactura"" ""Item1""
    JOIN ""fact"" ""Item2"" ON (""Item1"".""IdFactura"" = ""Item2"".""IdCliente"")

    UNION ALL

    SELECT 
        ""f"".*
    FROM ""conc"" ""f""
)
SELECT 
    ""x"".*
FROM ""conc"" ""x""
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void WithSimpleAlias()
        {
            var with =
            Sql.With(
                 Sql.From<Cliente>()
                 .Select(x => x)
             ).With(clie =>
                 Sql
                 .From<Factura>()
                 .Inner().Join(clie).OnTuple(y => y.Item1.IdCliente == y.Item2.IdRegistro)
                 .Select(z => new
                 {
                     z.Item1.IdCliente,
                     z.Item2.Nombre,
                 })
             ).Map((a, b) => new
             {
                 cliente = a,
                 facturas = b
             })
             .With(w => Sql
                .From<ConceptoFactura>()
                .Inner().Join(w.facturas).OnTuple(x => true)
                .Select(x => x.Item1)
            )
             .Map((a, b) => new
             {
                 cli = a.cliente,
                 fac = a.facturas,
                 con = b
             }).Query(w =>

                Sql.From(w.cli)
                .Select(x => x)
             );

            var ret = SqlWith.ApplyReplace(with.With, new ExprRep[0], null, with.Select.Parameters[0], ParamMode.EntityFramework, new SqlParamDic());
            var expected = @"
WITH ""cli"" AS (
    SELECT 
        ""x"".*
    FROM ""Cliente"" ""x""
)
, 
""fac"" AS (
    SELECT 
        ""Item1"".""IdCliente"" AS ""IdCliente"", 
        ""Item2"".""Nombre"" AS ""Nombre""
    FROM ""Factura"" ""Item1""
    JOIN ""cli"" ""Item2"" ON (""Item1"".""IdCliente"" = ""Item2"".""IdRegistro"")
)
, 
""con"" AS (
    SELECT 
        ""Item1"".*
    FROM ""ConceptoFactura"" ""Item1""
    JOIN ""fac"" ""Item2"" ON True
)
";
            AssertSql.AreEqual(expected, ret);

            var selectActual = with.ToSql().Sql;
            var selectExpected = @"
WITH ""cli"" AS (
    SELECT 
        ""x"".*
    FROM ""Cliente"" ""x""
), ""fac"" AS (
    SELECT 
        ""Item1"".""IdCliente"" AS ""IdCliente"", 
        ""Item2"".""Nombre"" AS ""Nombre""
    FROM ""Factura"" ""Item1""
    JOIN ""cli"" ""Item2"" ON (""Item1"".""IdCliente"" = ""Item2"".""IdRegistro"")
), ""con"" AS (
    SELECT 
        ""Item1"".*
    FROM ""ConceptoFactura"" ""Item1""
    JOIN ""fac"" ""Item2"" ON True
)

SELECT 
    ""x"".*
FROM ""cli"" ""x""
";

            AssertSql.AreEqual(selectExpected, selectActual);
        }
    }
}
