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
    public class WindowTest
    {
        [TestMethod]
        public void WindowOver()
        {
            var r = Sql
                .From(new SqlTable<Cliente>())
                .Window(win => new
                {
                    w1 = win.Rows().UnboundedPreceding().AndCurrentRow()
                })
                .Select((x, win) => new
                {
                    nom = x.Nombre,
                    ids = Sql.Over(Sql.Sum(x.Nombre), win.w1)
                });

            var actual = SqlText.SqlSelect.SelectToString(r.Clause);
            var expected = @"
SELECT 
    x.""Nombre"" AS ""nom"",
    sum(x.""Nombre"") OVER ""w1"" AS ""ids""
FROM ""Cliente"" x
WINDOW ""w1"" AS (ROWS UNBOUNDED PRECEDING AND CURRENT ROW)
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SimpleWindow()
        {
            var r = Sql
              .From(new SqlTable<Cliente>())
              .Window(win => new
              {
                  win1 = 
                      win
                      .Rows()
                      .UnboundedPreceding()
                      .AndCurrentRow()
                      .ExcludeNoOthers()
              })
              .Select(x => new
              {
                  nom = x.Nombre,
                  edo = x.IdEstado
              });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToString(clause);
            var expected = @"
SELECT ""x"".""Nombre"" AS ""nom"", ""x"".""IdEstado"" AS ""edo""
FROM ""Cliente"" ""x""
WINDOW ""win1"" AS (
ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW EXCLUDE NO OTHERS
)
";
            AssertSql.AreEqual(expected, actual);
        }
    }
    }
