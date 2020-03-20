using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sql2Sql.Fluent;
using Sql2Sql.SqlText;
using Sql2Sql.Tests;

namespace Sql2Sql.Test
{
    [TestClass]
    public class WindowTest
    {
        [TestMethod]
        public void WindowOver()
        {
            var r = Sql
                .From<Cliente>()
                .Window(win => new
                {
                    w1 = win.Rows().UnboundedPreceding().AndCurrentRow()
                })
                .Select((x, win) => new
                {
                    nom = x.Nombre,
                    ids = Sql.Over(Sql.Sum(x.Nombre), win.w1)
                });

            var actual = SqlText.SqlSelect.SelectToStringSP(r.Clause);
            var expected = @"
SELECT 
    ""x"".""Nombre"" AS ""nom"",
    sum(""x"".""Nombre"") OVER ""w1"" AS ""ids""
FROM ""Cliente"" ""x""
WINDOW ""w1"" AS ( ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW )
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SimpleWindow()
        {
            var r = Sql
              .From<Cliente>()
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
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT ""x"".""Nombre"" AS ""nom"", ""x"".""IdEstado"" AS ""edo""
FROM ""Cliente"" ""x""
WINDOW ""win1"" AS (
ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW EXCLUDE NO OTHERS
)
";
            AssertSql.AreEqual(expected, actual);
        }


        [TestMethod]
        public void EmptyWindow()
        {
            var r = Sql
              .From<Cliente>()
              .Window(win => new
              {
                  win1 =
                      win
                      .Rows()
                      .UnboundedPreceding()
                      .AndCurrentRow()
                      .ExcludeNoOthers(),
                   
              })
              .Select(x => new
              {
                  nom = x.Nombre,
                  edo = x.IdEstado
              });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT ""x"".""Nombre"" AS ""nom"", ""x"".""IdEstado"" AS ""edo""
FROM ""Cliente"" ""x""
WINDOW ""win1"" AS (
ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW EXCLUDE NO OTHERS
)
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ExistingWindow()
        {
            var r = Sql
              .From<Cliente>()
              .Window(win => new
              {
                  win1 =
                      win
                      .PartitionBy(x => x.IdEstado)

              })
              .Window((win, old) => new
              {
                  old.win1,
                  win2 = win.Existing(old.win1)
                  .Rows().CurrentRow().AndUnboundedFollowing()

              })
              .Select(x => new
              {
                  nom = x.Nombre,
                  edo = x.IdEstado
              });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
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
