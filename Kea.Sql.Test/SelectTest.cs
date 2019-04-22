using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeaSql;
using KeaSql.Tests;
using LinqKit;
using KeaSql.Fluent;

namespace KeaSql.Test
{
    [TestClass]
    public class SelectTest
    {
        [TestMethod]
        public void SelectLateralStarNamedSubq()
        {
            var query =
                Sql.From(
                    Sql
                    .FromTable<Cliente>()
                    .Select(q => new
                    {
                        q,
                        q.Nombre
                    })
                )
                .Inner().Lateral(q =>
                    Sql
                    .FromTable<Factura>()
                    .Inner().Join(new SqlTable<ConceptoFactura>()).OnTuple(x => true)
                    .Select(x => x)
                    .Where(x => x.Item1.IdCliente == q.q.IdRegistro)
                ).OnMap((a, b) => new
                {
                    cli = a,
                    fac = b
                }, x => true)
                .Select(x => x)
                ;

            var actual = query.ToSql().Sql;
            var expected = @"
SELECT 
    *
FROM (
    SELECT 
        ""q"".*, 
        ""q"".""Nombre"" AS ""Nombre""
    FROM ""Cliente"" ""q""
) ""cli""
JOIN LATERAL (
    SELECT 
        *
    FROM ""Factura"" ""Item1""
    JOIN ""ConceptoFactura"" ""Item2"" ON True
    WHERE (""Item1"".""IdCliente"" = ""cli"".""IdRegistro"")
) ""fac"" ON True
";

            AssertSql.AreEqual(expected, actual);
        }


        [TestMethod]
        public void SelectLateralStar()
        {
            var query =
                Sql.From(
                    Sql
                    .FromTable<Cliente>()
                    .Select(q => new
                    {
                        q,
                        q.Nombre
                    })
                )
                .Inner().Lateral(q =>
                    Sql
                    .FromTable<Factura>()
                    .Select(x => x)
                    .Where(x => x.IdCliente == q.q.IdRegistro)
                ).OnMap((a, b) => new
                {
                    cli = a,
                    fac = b
                }, x => true)
                .Select(x => x)
                ;

            var actual = query.ToSql().Sql;
            var expected = @"
SELECT 
    *
FROM (
    SELECT 
        ""q"".*, 
        ""q"".""Nombre"" AS ""Nombre""
    FROM ""Cliente"" ""q""
) ""cli""
JOIN LATERAL (
    SELECT 
        ""x"".*
    FROM ""Factura"" ""x""
    WHERE (""x"".""IdCliente"" = ""cli"".""IdRegistro"")
) ""fac"" ON True
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectJoinAlias()
        {
            var q = Sql
                .FromTable<Cliente>()
                .Inner().Join(new SqlTable<Factura>()).OnTuple(x => x.Item2.IdCliente == x.Item1.IdRegistro)
                .Alias(x => new
                {
                    a = x.Item1,
                    b = x.Item2
                })
                .Select(x => new
                {
                    cli = x.a.Nombre,
                    fac = x.b.Folio
                })
                ;

            var actual = q.ToSql();
        }


        [TestMethod]
        public void SubqueryJoinNamedFromStarSimple()
        {
            var r = Sql.From(
                    Sql
                    .From(new SqlTable<Cliente>())
                    .Left().Join(new SqlTable<Factura>())
                    .OnMap((a, b) => new
                    {
                        cli = a,
                        fac = b
                    }, x => x.cli.IdRegistro == x.fac.IdCliente)
                    .Select(x => x)
                )
                .Inner().Join(new SqlTable<ConceptoFactura>()).OnMap((a, b) => new
                {
                    clien = a,
                    conce = b
                }, x => x.conce.IdFactura == x.clien.fac.IdRegistro)
                .Select(y => new
                {
                    edo = y.clien.cli.IdEstado,
                    nom = y.clien.fac.Folio,
                    idc = y.conce.IdRegistro,
                    y.clien,
                    y.conce
                });
            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    ""clien"".""IdEstado"" AS ""edo"", 
    ""clien"".""Folio"" AS ""nom"", 
    ""conce"".""IdRegistro"" AS ""idc"", 
    ""clien"".*, 
    ""conce"".*
FROM (
    SELECT *
    FROM ""Cliente"" ""cli""
    LEFT JOIN ""Factura"" ""fac"" ON (""cli"".""IdRegistro"" = ""fac"".""IdCliente"")
) ""clien""
JOIN ""ConceptoFactura"" ""conce"" ON (""conce"".""IdFactura"" = ""clien"".""fac"".""IdRegistro"")
";
            AssertSql.AreEqual(expected, actual);
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void SubqueryJoinNamedFromStarSimpleEx()
        {
            var r = Sql.From(
                    Sql
                    .From(new SqlTable<Cliente>())
                    .Left().Join(new SqlTable<Factura>())
                    .OnMap((a, b) => new
                    {
                        cli = a,
                        fac = b
                    }, x => x.cli.IdRegistro == x.fac.IdCliente)
                    .Select(x => x)
                )
                .Inner().Join(new SqlTable<ConceptoFactura>()).OnMap((a, b) => new
                {
                    clien = a.cli,
                    factu = a.fac,
                    conce = b
                }, x => x.conce.IdFactura == x.factu.IdRegistro)
                .Select(y => new
                {
                    edo = y.clien.IdEstado,
                    nom = y.factu.Folio,
                    idc = y.conce.IdRegistro
                });
            var clause = r.Clause;
            //Debe de lanza excepción ya que esta mal definido el ON del JOIN
            SqlText.SqlSelect.SelectToStringSP(clause);

        }

        [TestMethod]
        public void JoinLateral()
        {
            var r = 
Sql
.FromTable<Factura>()
.Left().Lateral(fac =>
    Sql.FromTable<ConceptoFactura>()
    .Select(x => new
    {
        Total = Sql.Sum(x.Precio * x.Cantidad)
    })
    .Where(con => con.IdFactura == fac.IdRegistro)
)
.OnTuple(x => true)
.Alias(x => new
{
    fac = x.Item1,
    con = x.Item2
})
.Select(x => new
{
    IdFactura = x.fac.IdRegistro,
    Total = x.con.Total
});

            var actual = SqlText.SqlSelect.SelectToStringSP(r.Clause);
            var expected = @"
SELECT 
    ""fac"".""IdRegistro"" AS ""IdFactura"", 
    ""con"".""Total"" AS ""Total""
FROM ""Factura"" ""fac""
LEFT JOIN LATERAL (
    SELECT 
        sum((""x"".""Precio"" * ""x"".""Cantidad"")) AS ""Total""
    FROM ""ConceptoFactura"" ""x""
    WHERE (""x"".""IdFactura"" = ""fac"".""IdRegistro"")
) ""con"" ON True
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SubqueryNamedFromStarSimple()
        {
            var r = Sql.From(
                    Sql
                    .From(new SqlTable<Cliente>())
                    .Left().Join(new SqlTable<Factura>())
                    .OnTuple(x => x.Item1.IdRegistro == x.Item2.IdCliente)
                    .Select(x => x)
                )
                .Select(y => new
                {
                    edo = y.Item1.IdEstado,
                    nom = y.Item2.Folio
                });
            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    ""y"".""IdEstado"" AS ""edo"",
    ""y"".""Folio"" AS ""nom""
FROM (
    SELECT * 
    FROM ""Cliente"" ""Item1""
    LEFT JOIN ""Factura"" ""Item2"" ON (""Item1"".""IdRegistro"" = ""Item2"".""IdCliente"")
) ""y""
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SubqueryStarSimple()
        {
            var r = Sql.From(
                    Sql
                    .From(new SqlTable<Cliente>())
                    .Select(x => x)
                )
                .Select(y => new
                {
                    edo = y.IdEstado,
                    nom = y.Nombre
                });
            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    ""y"".""IdEstado"" AS ""edo"",
    ""y"".""Nombre"" AS ""nom""
FROM (
    SELECT ""x"".* FROM ""Cliente"" ""x""
) ""y""
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ScalarSubquery()
        {
            var q = Sql
                .FromTable<Cliente>()
                .Select(x => new
                {
                    idCli = x.IdRegistro,
                    fac = Sql
                    .FromTable<Factura>()
                    .Select(y => y.Folio)
                    .Where(y => y.IdCliente == x.IdRegistro)
                    .Scalar()
                });

            var actual = SqlText.SqlSelect.SelectToStringSP(q.Clause);
            var expected = @"
SELECT 
    ""x"".""IdRegistro"" AS ""idCli"", 
    (SELECT ""y"".""Folio"" FROM ""Factura"" ""y"" WHERE (""y"".""IdCliente"" = ""x"".""IdRegistro"")) AS ""fac""
FROM ""Cliente"" ""x""
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ScalarSelect()
        {
            var q = Sql
                .FromTable<Cliente>()
                .Select(x => x.IdRegistro);

            var actual = SqlText.SqlSelect.SelectToStringScalar(q.Clause, SqlText.ParamMode.None, new SqlText.SqlParamDic());
            var expected = @"
SELECT ""x"".""IdRegistro""
FROM ""Cliente"" ""x""
";
            Assert.IsTrue(actual.scalar);
            AssertSql.AreEqual(expected, actual.sql);
        }

        [TestMethod]
        public void NamedJoinLateral()
        {
            var q = Sql
                .FromTable<Cliente>()
                .Left().Join(new SqlTable<Factura>()).OnMap((a, b) => new
                {
                    cli = a,
                    fac = b
                }, x => x.cli.IdRegistro == x.fac.IdCliente)
                .Left().Lateral(y =>
                        Sql.FromTable<ConceptoFactura>()
                        .Select(z => z)
                        .Where(w => w.IdFactura == y.cli.IdRegistro)
                ).OnMap((c, d) => new
                {
                    clien = c.cli,
                    factu = c.fac,
                    conce = d
                }, e => true)
                .Select(r => r)
                ;

            var actual = SqlText.SqlSelect.SelectToStringSP(q.Clause);
            var expected = @"
SELECT *
FROM ""Cliente"" ""clien""
LEFT JOIN ""Factura"" ""factu"" ON (""clien"".""IdRegistro"" = ""factu"".""IdCliente"")
LEFT JOIN LATERAL 
(
    SELECT ""z"".*
    FROM ""ConceptoFactura"" ""z""
    WHERE (""z"".""IdFactura"" = ""clien"".""IdRegistro"")
) ""conce"" ON True
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SimpleJoinLateral()
        {
            var q = Sql.FromTable<Cliente>()
            .Left().Lateral(c =>
                Sql.FromTable<Factura>()
                .Select(x => x)
                .Where(y => y.IdCliente == c.IdRegistro)

            ).OnMap((a, b) => new
            {
                cliente = a,
                factura = b
            }, z => z.cliente.IdRegistro == z.factura.IdCliente)
            .Select(w => new
            {
                cliNom = w.cliente.Nombre,
                facFol = w.factura.Folio
            });

            var actual = SqlText.SqlSelect.SelectToStringSP(q.Clause);
            var expected = @"
SELECT ""cliente"".""Nombre"" AS ""cliNom"", ""factura"".""Folio"" AS ""facFol""
FROM ""Cliente"" ""cliente""
LEFT JOIN LATERAL
(
    SELECT *
    FROM ""Factura""
    WHERE(""IdCliente"" = ""cliente"".""IdRegistro"")
) ""factura"" ON (""cliente"".""IdRegistro"" = ""factura"".""IdCliente"")

";

        }

        [TestMethod]
        public void SimpleSelect()
        {
            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  nom = x.Nombre,
                  edo = x.IdEstado
              });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    ""x"".""Nombre"" AS ""nom"", 
    ""x"".""IdEstado"" AS ""edo""
FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ExprInvokeSelect()
        {
            Expression<Func<int, bool>> es10 = x => x == 10;

            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  nom = x.Nombre,
                  edo = x.IdEstado
              })
              .Where(x => es10.Invoke(x.IdRegistro))
              ;

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    ""x"".""Nombre"" AS ""nom"", 
    ""x"".""IdEstado"" AS ""edo""
FROM ""Cliente"" ""x""
WHERE (""x"".""IdRegistro"" = 10)
";
            AssertSql.AreEqual(expected, actual);
        }


        [TestMethod]
        public void SimpleGroupBy()
        {
            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  nom = x.Nombre,
                  edo = x.IdEstado
              })
              .GroupBy(x => x.IdEstado).ThenBy(x => x.Nombre)

              ;

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    ""x"".""Nombre"" AS ""nom"", 
    ""x"".""IdEstado"" AS ""edo""
FROM ""Cliente"" ""x""
GROUP BY ""x"".""IdEstado"", ""x"".""Nombre""
";
            AssertSql.AreEqual(expected, actual);
        }


        [TestMethod]
        public void StarSelect()
        {
            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => x);

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT ""x"".* FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SimpleJoinSelect()
        {
            var r = Sql
              .From(new SqlTable<Cliente>())
              .Inner().Join(new SqlTable<Estado>()).OnMap((a, b) => new
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
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
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
            var r = Sql
                .From(new SqlTable<Cliente>())
                .Inner().Join(new SqlTable<Estado>()).OnMap((a, b) => new
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
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
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
            var r = Sql
                .From(new SqlTable<Cliente>())
                .Select(x => new
                {
                    cli = x,
                    edo = x.IdEstado
                });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);

            var expected = @"
SELECT ""x"".*, ""x"".""IdEstado"" AS ""edo""
FROM ""Cliente"" ""x""
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SubquerySimple()
        {
            var r = Sql.From(
                    Sql
                    .From(new SqlTable<Cliente>())
                    .Select(x => x)
                )
                .Select(y => y);
            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
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
            Sql.From(
                    Sql
                 .From(new SqlTable<Cliente>())
                 .Inner().Join(new SqlTable<Estado>()).OnMap((a, b) => new
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
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
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
            Sql.From(
                    Sql
                 .From(new SqlTable<Cliente>())
                 .Inner().Join(new SqlTable<Estado>()).OnMap((a, b) => new
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
            .Inner().Join(new SqlTable<Factura>()).OnMap((a, b) => new
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
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
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
