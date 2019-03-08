using System;
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
        public void StarSelect()
        {
            var r = Sql2
              .From(new SqlTable<Cliente>())
              .Select(x => x);

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToString(clause);
            var expected = @"
SELECT * FROM ""Cliente""
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
SELECT cli.""Nombre"" AS ""cliNomb"", edo.""IdRegistro"" AS ""edoId""
FROM ""Cliente"" cli
JOIN ""Estado"" edo ON (cli.""IdEstado"" = edo.""IdRegistro"")
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SqlMultiStar()
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
SELECT ""Cliente"".
";
        }
    }
}
