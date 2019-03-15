using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeaSql.Fluent;
using KeaSql.SqlText;
using KeaSql.Tests;

namespace KeaSql.Test
{
    [TestClass]
    public class FromListTest
    {
       

        [TestMethod]
        public void SimpleJoin()
        {
            var r = Sql
           .From(new SqlTable<Cliente>())
           .Inner().Join(new SqlTable<Estado>()).OnMap((a, b) => new
           {
               cli = a,
               edo = b
           }, x => x.cli.IdEstado == x.edo.IdRegistro)
           ;
            var actual = SqlFromList.FromListToStrSP(r.Clause.From, "q", false).Sql;
            var expected = @"
FROM ""Cliente"" ""cli""
JOIN ""Estado"" ""edo"" ON (""cli"".""IdEstado"" = ""edo"".""IdRegistro"")
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MultiJoin()
        {
            var r = Sql
               .From(new SqlTable<Cliente>())
               .Inner().Join(new SqlTable<Estado>()).OnMap((a, b) => new
               {
                   cli = a,
                   edo = b
               }, x => x.cli.IdEstado == x.edo.IdRegistro)
               .Inner().Join(new SqlTable<Factura>()).OnMap((c, d) => new
               {
                   cliente = c.cli,
                   estado = c.edo,
                   factura = d
               }, y => y.cliente.IdRegistro == y.factura.IdCliente)
               .Inner().Join(new SqlTable<ConceptoFactura>()).OnMap((e, f) => new
               {
                   clien = e.cliente,
                   fact = e.factura,
                   concepto = f
               }, z => z.concepto.IdFactura == z.fact.IdRegistro)
               ;
            var actual = SqlFromList.FromListToStrSP(r.Clause.From, "q", false).Sql;

            var expected = @"
FROM ""Cliente"" ""clien""
JOIN ""Estado"" ""estado"" ON (""clien"".""IdEstado"" = ""estado"".""IdRegistro"")
JOIN ""Factura"" ""fact"" ON (""clien"".""IdRegistro"" = ""fact"".""IdCliente"")
JOIN ""ConceptoFactura"" ""concepto"" ON (""concepto"".""IdFactura"" = ""fact"".""IdRegistro"")
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void NameCollisionJoin()
        {
            var r = Sql
                .From(new SqlTable<Cliente>())
                .Inner().Join(new SqlTable<Estado>()).OnMap((a, b) => new
                {
                    a = a,
                    b = b
                }, x => x.a.IdEstado == x.b.IdRegistro)
                .Inner().Join(new SqlTable<Factura>()).OnMap((a, b) => new
                {
                    a = a.b,
                    b = b
                }, x => x.a.IdRegistro == x.b.IdRegistro)
                .Inner().Join(new SqlTable<ConceptoFactura>()).OnMap((a, b) => new
                {
                    a = a.b,
                    b = b
                }, x => x.a.IdCliente == x.b.IdFactura);
            ;



            var expected = @"
FROM ""Cliente"" ""a2""
JOIN ""Estado"" ""a1"" ON (""a2"".""IdEstado"" = ""a1"".""IdRegistro"")
JOIN ""Factura"" ""a"" ON (""a1"".""IdRegistro"" = ""a"".""IdRegistro"")
JOIN ""ConceptoFactura"" ""b"" ON (""a"".""IdCliente"" = ""b"".""IdFactura"")
";

            var actual = SqlFromList.FromListToStrSP(r.Clause.From, "q", false).Sql;
            AssertSql.AreEqual(expected, actual);
        }



        [TestMethod]
        public void SimpleAliasJoin()
        {
            var r = Sql
                .From(new SqlTable<Cliente>())
                .Inner().Join(new SqlTable<Estado>()).OnMap(
                    (a,b) => new Tuple<Cliente,Estado>(a, b)
                ,y => y.Item1.IdEstado == y.Item2.IdRegistro)
                .Alias(y => new
                {
                    cli = y.Item1,
                    edo = y.Item2
                })
                ;

            var expected = @"
FROM ""Cliente"" ""cli""
JOIN ""Estado"" ""edo"" ON (""cli"".""IdEstado"" = ""edo"".""IdRegistro"")
";

            var actual = SqlFromList.FromListToStrSP(r.Clause.From, "q", false).Sql;
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SimpleAliasJoin2()
        {
            var r = Sql
                .From(new SqlTable<Cliente>())
                .Inner().Join(new SqlTable<Estado>()).OnTuple(x => x.Item1.IdEstado == x.Item2.IdRegistro)
                .Inner().Join(new SqlTable<Factura>()).On(x => x.Item1.IdRegistro == x.Item3.IdCliente)
                .Alias(x => new
                {
                    cli = x.Item1,
                    edo = x.Item2,
                    fac = x.Item3
                })
                ;

            var expected = @"
FROM ""Cliente"" ""cli""
JOIN ""Estado"" ""edo"" ON (""cli"".""IdEstado"" = ""edo"".""IdRegistro"")
JOIN ""Factura"" ""fac"" ON (""cli"".""IdRegistro"" = ""fac"".""IdCliente"")
";

            var actual = SqlFromList.FromListToStrSP(r.Clause.From, "q", false).Sql;
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SimpleAliasJoinSelect()
        {
            var r = Sql
                .From(new SqlTable<Cliente>())
                .Inner().Join(new SqlTable<Estado>()).OnTuple(x => x.Item1.IdEstado == x.Item2.IdRegistro)
                .Inner().Join(new SqlTable<Factura>()).On(x => x.Item1.IdRegistro == x.Item3.IdCliente)
                .Alias(x => new
                {
                    cli = x.Item1,
                    edo = x.Item2,
                    fac = x.Item3
                })
                .Select(x => new
                {
                    idCli= x.cli.IdRegistro,
                    idEdo = x.edo.IdRegistro
                })
                ;

            var expected = @"
SELECT ""cli"".""IdRegistro"" AS ""idCli"", ""edo"".""IdRegistro"" AS ""idEdo""
FROM ""Cliente"" ""cli""
JOIN ""Estado"" ""edo"" ON (""cli"".""IdEstado"" = ""edo"".""IdRegistro"")
JOIN ""Factura"" ""fac"" ON (""cli"".""IdRegistro"" = ""fac"".""IdCliente"")
";

            var actual = SqlSelect.SelectToStringSP(r.Clause);
            AssertSql.AreEqual(expected, actual);
        }
    }
}
