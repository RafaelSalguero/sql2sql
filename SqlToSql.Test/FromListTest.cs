using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlToSql.Fluent;
using SqlToSql.SqlText;

namespace SqlToSql.Test
{
    [TestClass]
    public class FromListTest
    {
       

        [TestMethod]
        public void SimpleJoin()
        {
            var r = Sql2
           .From(new SqlTable<Cliente>())
           .Join(new SqlTable<Estado>()).On((a, b) => new
           {
               cli = a,
               edo = b
           }, x => x.cli.IdEstado == x.edo.IdRegistro)
           ;
            var actual = SqlFromList.FromListToStr(r.Clause.From, "q").Sql;
            var expected = @"
FROM ""Cliente"" ""cli""
JOIN ""Estado"" ""edo"" ON (""cli"".""IdEstado"" = ""edo"".""IdRegistro"")
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MultiJoin()
        {
            var r = Sql2
               .From(new SqlTable<Cliente>())
               .Join(new SqlTable<Estado>()).On((a, b) => new
               {
                   cli = a,
                   edo = b
               }, x => x.cli.IdEstado == x.edo.IdRegistro)
               .Join(new SqlTable<Factura>()).On((c, d) => new
               {
                   cliente = c.cli,
                   estado = c.edo,
                   factura = d
               }, y => y.cliente.IdRegistro == y.factura.IdCliente)
               .Join(new SqlTable<ConceptoFactura>()).On((e, f) => new
               {
                   clien = e.cliente,
                   fact = e.factura,
                   concepto = f
               }, z => z.concepto.IdFactura == z.fact.IdRegistro)
               ;
            var actual = SqlFromList.FromListToStr(r.Clause.From, "q").Sql;

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
            var r = Sql2
                .From(new SqlTable<Cliente>())
                .Join(new SqlTable<Estado>()).On((a, b) => new
                {
                    a = a,
                    b = b
                }, x => x.a.IdEstado == x.b.IdRegistro)
                .Join(new SqlTable<Factura>()).On((a, b) => new
                {
                    a = a.b,
                    b = b
                }, x => x.a.IdRegistro == x.b.IdRegistro)
                .Join(new SqlTable<ConceptoFactura>()).On((a, b) => new
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

            var actual = SqlFromList.FromListToStr(r.Clause.From, "q").Sql;
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SimpleAliasJoin()
        {
            var r = Sql2
                .From(new SqlTable<Cliente>())
                .Join(new SqlTable<Estado>()).On(x => x.Item1.IdEstado == x.Item2.IdRegistro)
                .Join(new SqlTable<Factura>()).On(x => x.Item1.IdRegistro == x.Item3.IdCliente)
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

            var actual = SqlFromList.FromListToStr(r.Clause.From, "q").Sql;
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SimpleAliasJoinSelect()
        {
            var r = Sql2
                .From(new SqlTable<Cliente>())
                .Join(new SqlTable<Estado>()).On(x => x.Item1.IdEstado == x.Item2.IdRegistro)
                .Join(new SqlTable<Factura>()).On(x => x.Item1.IdRegistro == x.Item3.IdCliente)
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

            var actual = SqlSelect.SelectToString(r.Clause);
            AssertSql.AreEqual(expected, actual);
        }
    }
}
