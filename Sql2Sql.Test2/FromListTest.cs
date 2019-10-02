using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sql2Sql.Fluent;
using Sql2Sql.SqlText;
using Sql2Sql.Tests;

namespace Sql2Sql.Test
{
    [TestClass]
    public class FromListTest
    {


        [TestMethod]
        public void SimpleJoin()
        {
            var r = Sql
           .From(new SqlTable<Cliente>())
           .Inner().Join(new SqlTable<Estado>()).On(x => x.Item1.IdEstado == x.Item2.IdRegistro)
           .Alias(x => new
           {
               cli = x.Item1,
               edo = x.Item2
           })
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
               .From<Cliente>()
               .Join<Estado>().On(x => x.Item1.IdEstado == x.Item2.IdRegistro)
               .Join<Factura>().On(x => x.Item1.IdRegistro == x.Item3.IdCliente)
               .Join<ConceptoFactura>().On(x => x.Item4.IdFactura == x.Item3.IdRegistro)
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
                .From<Cliente>()
                .Join<Estado>().On(x => x.Item1.IdEstado == x.Item2.IdRegistro)
                .Join<Factura>().On(x => x.Item1.IdRegistro == x.Item3.IdRegistro)
                .Join<ConceptoFactura>().On(x => x.Item3.IdCliente == x.Item4.IdFactura)
                .Alias(x => new
                {
                    a2 = x.Item1,
                    a1 = x.Item2,
                    a = x.Item3,
                    b = x.Item4
                })
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
                .From<Cliente>()
                .Join<Estado>().On(x => x.Item1.IdEstado == x.Item2.IdRegistro)
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
                .From<Cliente>()
                .Join<Estado>().On(x => x.Item1.IdEstado == x.Item2.IdRegistro)
                .Join<Factura>().On(x => x.Item1.IdRegistro == x.Item3.IdCliente)
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
                .From<Cliente>()
                .Join<Estado>().On(x => x.Item1.IdEstado == x.Item2.IdRegistro)
                .Join<Factura>().On(x => x.Item1.IdRegistro == x.Item3.IdCliente)
                .Alias(x => new
                {
                    cli = x.Item1,
                    edo = x.Item2,
                    fac = x.Item3
                })
                .Select(x => new
                {
                    idCli = x.cli.IdRegistro,
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
