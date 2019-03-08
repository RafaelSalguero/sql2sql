﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlToSql.Fluent;

namespace SqlToSql.Test
{
    [TestClass]
    public class SelectTest
    {
        [TestMethod]
        public void SimpleSelect()
        {
            var r = Sql2
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  nom = x.Nombre,
                  edo = x.IdEstado
              });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToString(clause);
            var expected = @"
SELECT 
    ""x"".""Nombre"" AS ""nom"", 
    ""x"".""IdEstado"" AS ""edo""
FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);
        }


        [TestMethod]
        public void StarSelect()
        {
            var r = Sql2
              .From(new SqlTable<Cliente>())
              .Select(x => x);

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToString(clause);
            var expected = @"
SELECT ""x"".* FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SimpleJoinSelect()
        {
            var r = Sql2
              .From(new SqlTable<Cliente>())
              .Join(new SqlTable<Estado>()).On((a, b) => new
              {
                  cli = a,
                  edo = b
              }, x => x.cli.IdEstado == x.edo.IdRegistro)
              .Select(x => new
              {
                  cliNomb = x.cli.Nombre,
                  edoId = x.edo.IdRegistro
              });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToString(clause);
            var expected = @"
SELECT ""cli"".""Nombre"" AS ""cliNomb"", ""edo"".""IdRegistro"" AS ""edoId""
FROM ""Cliente"" ""cli""
JOIN ""Estado"" ""edo"" ON (""cli"".""IdEstado"" = ""edo"".""IdRegistro"")
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SqlMultiStar()
        {
            var r = Sql2
                .From(new SqlTable<Cliente>())
                .Join(new SqlTable<Estado>()).On((a, b) => new
                {
                    cli = a,
                    edo = b
                }, x => x.cli.IdEstado == x.edo.IdRegistro)
                .Select(x => new
                {
                    cli = x.cli,
                    edo = x.edo,
                    nom = x.cli.Nombre,
                    idEdo = x.edo.IdRegistro
                });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToString(clause);
            var expected = @"
SELECT ""cli"".*, ""edo"".*, ""cli"".""Nombre"" AS ""nom"", ""edo"".""IdRegistro"" AS ""idEdo""
FROM ""Cliente"" ""cli""
JOIN ""Estado"" ""edo"" ON (""cli"".""IdEstado"" = ""edo"".""IdRegistro"")
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SqlStartMultiCol()
        {
            var r = Sql2
                .From(new SqlTable<Cliente>())
                .Select(x => new
                {
                    cli = x,
                    edo = x.IdEstado
                });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToString(clause);

            var expected = @"
SELECT ""x"".*, ""x"".""IdEstado"" AS ""edo""
FROM ""Cliente"" ""x""
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SubquerySimple()
        {
            var r = Sql2.From(
                    Sql2
                    .From(new SqlTable<Cliente>())
                    .Select(x => x)
                )
                .Select(y => y);
            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToString(clause);
            var expected = @"
SELECT ""y"".*
FROM (
    SELECT ""x"".* FROM ""Cliente"" ""x""
) ""y""
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SubquerySimpleJoin()
        {
            var r =
            Sql2.From(
                    Sql2
                 .From(new SqlTable<Cliente>())
                 .Join(new SqlTable<Estado>()).On((a, b) => new
                 {
                     cli = a,
                     edo = b
                 }, x => x.cli.IdEstado == x.edo.IdRegistro)
                 .Select(x => new
                 {
                     cliNomb = x.cli.Nombre,
                     edoId = x.edo.IdRegistro
                 })
            )
            .Select(subQ => new
            {
                idEdo = subQ.edoId,
                cliN = subQ.cliNomb
            });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToString(clause);
            var expected = @"
SELECT 
    ""subQ"".""edoId"" AS ""idEdo"",
    ""subQ"".""cliNomb"" AS ""cliN""
FROM (
    SELECT ""cli"".""Nombre"" AS ""cliNomb"", ""edo"".""IdRegistro"" AS ""edoId""
    FROM ""Cliente"" ""cli""
    JOIN ""Estado"" ""edo"" ON (""cli"".""IdEstado"" = ""edo"".""IdRegistro"")    
) ""subQ""
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SubquerySimpleJoinOutterJoin()
        {
            var r =
            Sql2.From(
                    Sql2
                 .From(new SqlTable<Cliente>())
                 .Join(new SqlTable<Estado>()).On((a, b) => new
                 {
                     cli = a,
                     edo = b
                 }, x => x.cli.IdEstado == x.edo.IdRegistro)
                 .Select(x => new
                 {
                     cliId = x.cli.IdRegistro,
                     edoId = x.edo.IdRegistro
                 })
            )
            .Join(new SqlTable<Factura>()).On((a, b) => new
            {
                sq = a,
                fac = b
            }, x => x.fac.IdCliente == x.sq.cliId)
            .Select(subQ => new
            {
                idEdo = subQ.sq.edoId,
                cliN = subQ.sq.cliId,
                fac = subQ.fac.Folio
            });


            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToString(clause);
            var expected = @"
SELECT 
    ""sq"".""edoId"" AS ""idEdo"",
    ""sq"".""cliId"" AS ""cliN"",
    ""fac"".""Folio"" AS ""fac""
FROM (
    SELECT ""cli"".""IdRegistro"" AS ""cliId"", ""edo"".""IdRegistro"" AS ""edoId""
    FROM ""Cliente"" ""cli""
    JOIN ""Estado"" ""edo"" ON (""cli"".""IdEstado"" = ""edo"".""IdRegistro"")    
) ""sq""
JOIN ""Factura"" ""fac"" ON (""fac"".""IdCliente"" = ""sq"".""cliId"")
";
            AssertSql.AreEqual(expected, actual);
        }


    }
}
