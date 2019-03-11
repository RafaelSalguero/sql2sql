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
        public void SimpleWindow()
        {
            var r = Sql2
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
