﻿using System;
using System.Linq;
using System.Linq.Expressions;
using Sql2Sql.Fluent;
using Sql2Sql.Tests;
using LinqKit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sql2Sql.Test
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
                    .From<Cliente>()
                    .Select(q => new
                    {
                        q,
                        q.Nombre
                    })
                )
                .Lateral(q =>
                    Sql
                    .From<Factura>()
                    .Join<ConceptoFactura>().On(x => true)
                    .Select(x => x)
                    .Where(x => x.Item1.IdCliente == q.q.IdRegistro)
                ).On(x => true)
                .Alias(x => new
                {
                    cli = x.Item1,
                    fac = x.Item2,
                })
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
                    .From<Cliente>()
                    .Select(q => new
                    {
                        q,
                        q.Nombre
                    })
                )
                .Lateral(q =>
                    Sql
                    .From<Factura>()
                    .Select(x => x)
                    .Where(x => x.IdCliente == q.q.IdRegistro)
                ).On(x => true)
                .Alias(x => new
                {
                    cli = x.Item1,
                    fac = x.Item2,
                })
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
                .From<Cliente>()
                .Inner().Join(new SqlTable<Factura>()).On(x => x.Item2.IdCliente == x.Item1.IdRegistro)
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
        public void SubqueryExists()
        {
            var q = Sql
                .From<Cliente>()
                .Select(x => x)
                .Where(cli =>
                    Sql.Exists(
                            Sql
                            .From<Factura>()
                            .Select(fac => 1)
                            .Where(fac => fac.IdCliente == cli.IdRegistro)
                        )
                );

            var actual = q.ToSql().Sql;
            var expected = @"
SELECT 
    ""x"".*
FROM ""Cliente"" ""x""
WHERE EXISTS (
    SELECT 
        1
    FROM ""Factura"" ""fac""
    WHERE (""fac"".""IdCliente"" = ""x"".""IdRegistro"")
)
";

            AssertSql.AreEqual(expected, actual);
        }


        [TestMethod]
        public void SubqueryIn()
        {
            var q = Sql
               .From<Cliente>()
               .Select(x => x)
               .Where(cli =>
                   Sql.In(
                            1,
                           Sql
                           .From<Factura>()
                           .Select(fac => fac.IdRegistro)
                           .Where(fac => fac.IdCliente == cli.IdRegistro)
                       )
               );

            var actual = q.ToSql().Sql;
            var expected = @"
SELECT 
    ""x"".*
FROM ""Cliente"" ""x""
WHERE (1 IN (
    SELECT 
        ""fac"".""IdRegistro""
    FROM ""Factura"" ""fac""
    WHERE (""fac"".""IdCliente"" = ""x"".""IdRegistro"")
))
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SubqueryJoinNamedFromStarSimple()
        {
            var r = Sql.From(
                    Sql
                    .From<Cliente>()
                    .Left().Join<Factura>()
                    .On(x => x.Item1.IdRegistro == x.Item2.IdCliente)
                    .Alias(x => new
                    {
                        cli = x.Item1,
                        fac = x.Item2,
                    })
                    .Select(x => x)
                )
                .Join<ConceptoFactura>().On(x => x.Item2.IdFactura == x.Item1.fac.IdRegistro)
                .Alias(x => new
                {
                    clien = x.Item1,
                    conce = x.Item2
                })
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

        [TestMethod]
        public void JoinLateral()
        {
            var r =
Sql
.From<Factura>()
.Left().Lateral(fac =>
    Sql.From<ConceptoFactura>()
    .Select(x => new
    {
        Total = Sql.Sum(x.Precio * x.Cantidad)
    })
    .Where(con => con.IdFactura == fac.IdRegistro)
)
.On(x => true)
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
                    .On(x => x.Item1.IdRegistro == x.Item2.IdCliente)
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
                .From<Cliente>()
                .Select(x => new
                {
                    idCli = x.IdRegistro,
                    fac = Sql
                    .From<Factura>()
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
                .From<Cliente>()
                .Select(x => x.IdRegistro);

            var actual = SqlText.SqlSelect.SelectToStringScalar(q.Clause, SqlText.ParamMode.None, new SqlText.SqlParamDic());
            var expected = @"
SELECT ""x"".""IdRegistro""
FROM ""Cliente"" ""x""
";
            Assert.IsTrue(actual.Scalar);
            AssertSql.AreEqual(expected, actual.Sql);
        }

        [TestMethod]
        public void NamedJoinLateral()
        {
            var q = Sql
                .From<Cliente>()
                .Left().Join<Factura>().On(x => x.Item1.IdRegistro == x.Item2.IdCliente)
                .Left().Lateral(y =>
                        Sql.From<ConceptoFactura>()
                        .Select(z => z)
                        .Where(w => w.IdFactura == y.Item1.IdRegistro)
                )
                .On(x => true)
                .Alias(x => new
                {
                    clien = x.Item1,
                    factu = x.Item2,
                    conce = x.Item3
                })
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
            var q = Sql.From<Cliente>()
            .Left().Lateral(c =>
                Sql.From<Factura>()
                .Select(x => x)
                .Where(y => y.IdCliente == c.IdRegistro)

            )
            .On(x => x.Item1.IdRegistro == x.Item2.IdCliente)
            .Alias(x => new
            {
                cliente = x.Item1,
                factura = x.Item2
            })
            .Select(w => new
            {
                cliNom = w.cliente.Nombre,
                facFol = w.factura.Folio
            });

            var actual = SqlText.SqlSelect.SelectToStringSP(q.Clause);
            var expected = @"
SELECT 
    ""cliente"".""Nombre"" AS ""cliNom"", 
    ""factura"".""Folio"" AS ""facFol""
FROM ""Cliente"" ""cliente""
LEFT JOIN LATERAL (
    SELECT
        ""x"".*
    FROM ""Factura"" ""x""
    WHERE (""x"".""IdCliente"" = ""cliente"".""IdRegistro"")
) ""factura"" ON (""cliente"".""IdRegistro"" = ""factura"".""IdCliente"")
";

            AssertSql.AreEqual(expected, actual);
        }


        /// <summary>
        /// Comprueba que el cuerpo del JOIN LATERAL puede provenir de una función
        /// </summary>
        [TestMethod]
        public void JoinLateralSubqueryExpression()
        {
            Expression<Func<int, ISqlSelect<Factura>>> subqueryExpr = idCliente => Sql.From<Factura>()
            .Select(x => x)
            .Where(y => y.IdCliente == idCliente);

            var q = Sql.From<Cliente>()
            .Left().Lateral(c => subqueryExpr.Invoke(c.IdRegistro))
            .On(x => x.Item1.IdRegistro == x.Item2.IdCliente)
            .Alias(x => new
            {
                cliente = x.Item1,
                factura = x.Item2,
            })
            .Select(w => new
            {
                cliNom = w.cliente.Nombre,
                facFol = w.factura.Folio
            });

            var actual = SqlText.SqlSelect.SelectToStringSP(q.Clause);
            var expected = @"
SELECT 
    ""cliente"".""Nombre"" AS ""cliNom"", 
    ""factura"".""Folio"" AS ""facFol""
FROM ""Cliente"" ""cliente""
LEFT JOIN LATERAL (
    SELECT
        ""x"".*
    FROM ""Factura"" ""x""
    WHERE (""x"".""IdCliente"" = ""cliente"".""IdRegistro"")
) ""factura"" ON (""cliente"".""IdRegistro"" = ""factura"".""IdCliente"")
";

            AssertSql.AreEqual(expected, actual);
        }

        /// <summary>
        /// Comprueba que el cuerpo del JOIN LATERAL puede provenir de una función
        /// </summary>
        [TestMethod]
        public void JoinLateralSubqueryExpressionNeasted()
        {
            Expression<Func<int, ISqlSelect<Factura>>> queryFacturas = idCliente => Sql.From<Factura>()
            .Select(x => x)
            .Where(y => y.IdCliente == idCliente);

            Expression<Func<int, ISqlSelect<Factura>>> queryFacturas2 = idCliente =>
             Sql.From(queryFacturas.Invoke(idCliente))
            .Select(x => x)
            .Where(y => y.IdCliente == idCliente);

            Expression<Func<int, ISqlSelect<Factura>>> subqueryExpr = idCliente =>
            Sql.From(queryFacturas2.Invoke(idCliente))
            .Select(x => x)
            .Where(y => y.IdCliente == idCliente);

            var q = Sql.From<Cliente>()
            .Left().Lateral(c => subqueryExpr.Invoke(c.IdRegistro))
            .On(x => x.Item1.IdRegistro == x.Item2.IdCliente)
            .Alias(x => new
            {
                cliente = x.Item1,
                factura = x.Item2
            })
            .Select(w => new
            {
                cliNom = w.cliente.Nombre,
                facFol = w.factura.Folio
            });

            var actual = SqlText.SqlSelect.SelectToStringSP(q.Clause);
            var expected = @"
SELECT 
    ""cliente"".""Nombre"" AS ""cliNom"", 
    ""factura"".""Folio"" AS ""facFol""
FROM ""Cliente"" ""cliente""
LEFT JOIN LATERAL (
    SELECT 
        ""x"".*
    FROM (
        SELECT 
            ""x"".*
        FROM (
            SELECT 
                ""x"".*
            FROM ""Factura"" ""x""
            WHERE (""x"".""IdCliente"" = ""cliente"".""IdRegistro"")
        ) ""x""
        WHERE (""x"".""IdCliente"" = ""cliente"".""IdRegistro"")
    ) ""x""
    WHERE (""x"".""IdCliente"" = ""cliente"".""IdRegistro"")
) ""factura"" ON (""cliente"".""IdRegistro"" = ""factura"".""IdCliente"")
";

            AssertSql.AreEqual(expected, actual);
        }


        /// <summary>
        /// Devuelve el subquery de un JOIN lateral, note que las referencias a las columnas del lado izquierdo del JOIN lateral se puede pasar como
        /// argumentos en forma de expresión
        /// </summary>
        ISqlSelect<Factura> JoinLateralSubqueryFunction_QueryFacturas(Expression<Func<int>> idCliente)
        {
            return Sql.From<Factura>()
            .Select(x => x)
            .Where(y => y.IdCliente == idCliente.Invoke());
        }

        /// <summary>
        /// Comprueba que el cuerpo del JOIN LATERAL puede provenir de una función
        /// </summary>
        [TestMethod]
        public void JoinLateralSubqueryFunction()
        {
            var q = Sql.From<Cliente>()
            .Left().Lateral(c => JoinLateralSubqueryFunction_QueryFacturas(() => c.IdRegistro))
            .On(x => x.Item1.IdRegistro == x.Item2.IdCliente)
            .Alias(x => new
            {
                cliente = x.Item1,
                factura = x.Item2
            })
            .Select(w => new
            {
                cliNom = w.cliente.Nombre,
                facFol = w.factura.Folio
            });

            var actual = SqlText.SqlSelect.SelectToStringSP(q.Clause);
            var expected = @"
SELECT 
    ""cliente"".""Nombre"" AS ""cliNom"", 
    ""factura"".""Folio"" AS ""facFol""
FROM ""Cliente"" ""cliente""
LEFT JOIN LATERAL (
    SELECT
        ""x"".*
    FROM ""Factura"" ""x""
    WHERE (""x"".""IdCliente"" = ""cliente"".""IdRegistro"")
) ""factura"" ON (""cliente"".""IdRegistro"" = ""factura"".""IdCliente"")
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectSoloFrom()
        {
            IFromListItemTarget r = Sql
              .From(new SqlTable<Cliente>())
              ;


            var actual = r.ToSql().Sql;
            var expected = @"
SELECT 
    ""x"".*
FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);
        }


        [TestMethod]
        public void SelectSoloFromWhere()
        {
            IFromListItemTarget r = Sql
              .From(new SqlTable<Cliente>())
              .Where(x => x.IdRegistro == 10)
              ;


            var actual = r.ToSql().Sql;
            var expected = @"
SELECT 
    ""x"".*
FROM ""Cliente"" ""x""
WHERE (""x"".""IdRegistro"" = 10)
";
            AssertSql.AreEqual(expected, actual);
        }

        static ISqlSelect<T> QueryCliente<T>()
            where T : ICliente
        {
            var r = Sql
            .From<T>()
            .Where(x => x.Nombre == "Rafa");

            return r;
        }

        [TestMethod]
        public void SimpleWhereSinSelectInterface()
        {
            var r = QueryCliente<Cliente>();
            var actual = r.ToSql().Sql;
            var expected = @"
SELECT 
    ""x"".*
FROM ""Cliente"" ""x""
WHERE (""x"".""Nombre"" = 'Rafa')
";
            AssertSql.AreEqual(expected, actual);
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
        public void SimpleStar()
        {
            var r = Sql
                .From<Cliente>()
                .Select(x => Sql.Star().Map(new ClienteDTO
                {
                    NombreCompleto = x.Nombre + x.Apellido
                }))
                ;

            var actual = r.ToString();
            var expected = @"
SELECT 
    *,
    (""x"".""Nombre"" || ""x"".""Apellido"") AS ""NombreCompleto""  
FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void FromStar()
        {
            var r = Sql
                .From<Cliente>()
                .Select(x => Sql.Star(x).Map(new ClienteDTO
                {
                    NombreCompleto = x.Nombre + x.Apellido
                }))
                ;

            var actual = r.ToString();
            var expected = @"
SELECT 
    ""x"".*,
    (""x"".""Nombre"" || ""x"".""Apellido"") AS ""NombreCompleto""  
FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void JoinStar()
        {
            var r = Sql
                .From<Cliente>()
                .Join<Factura>()
                .On(x => x.Item2.IdCliente == x.Item1.IdRegistro)
                .Alias(x => new
                {
                    cli = x.Item1,
                    fac = x.Item2
                })
                .Select(x => Sql.Star(x.cli, x.fac).Map(new ClienteDTO
                {
                    NombreCompleto = x.cli.Nombre + x.cli.Apellido
                }))
                ;

            var actual = r.ToString();
            var expected = @"
SELECT 
    ""cli"".*, ""fac"".*, (""cli"".""Nombre"" || ""cli"".""Apellido"") AS ""NombreCompleto""
FROM ""Cliente"" ""cli""
JOIN ""Factura"" ""fac"" ON (""fac"".""IdCliente"" = ""cli"".""IdRegistro"")
";
            AssertSql.AreEqual(expected, actual);
        }

        /// <summary>
        /// Prueba que funcionen los parametros cuando existe un nulo en la ruta de parámetro
        /// </summary>
        [TestMethod]
        public void NullParamTest()
        {
            Cliente filtro = null;

            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  nom = x.Nombre,
                  edo = x.IdEstado
              })
              .Where(x => x.Fecha == filtro.Fecha)
              ;

            var actual = r.ToSql().Sql;
            var expected = @"
SELECT 
    ""x"".""Nombre"" AS ""nom"", 
    ""x"".""IdEstado"" AS ""edo""
FROM ""Cliente"" ""x""
WHERE (""x"".""Fecha"" = @Fecha)
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SimpleSelectFuncExpr()
        {
            var min = new DateTime(2019, 01, 26);
            var max = new DateTime(2019, 01, 30);

            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  fecha = SqlExpr.Range<DateTime>().Invoke(min, max, x.Fecha)
              });

            var actual = r.ToSql(SqlText.ParamMode.EntityFramework).Sql;
            var expected = @"
SELECT 
    ((""x"".""Fecha"" >= @min) AND (""x"".""Fecha"" <= @max)) AS ""fecha""
FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);
        }


        [TestMethod]
        public void EliminacionBooleanaConst()
        {
            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  nom = x.Nombre,
                  edo = x.IdEstado
              })
              .Where(x =>
                SqlExpr.EqualsNullable.Invoke(x.IdRegistro, null) &&
                SqlExpr.EqualsNullable.Invoke(x.IdEstado, 123)
            )
              ;

            var actual = r.ToSql(SqlText.ParamMode.Substitute).Sql;
            var expected = @"
SELECT 
    ""x"".""Nombre"" AS ""nom"", 
    ""x"".""IdEstado"" AS ""edo""
FROM ""Cliente"" ""x""
WHERE (""x"".""IdEstado"" = 123)
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EliminacionBooleanaParam()
        {
            int? idRegistro = null;
            int? idEstado = 123;

            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  nom = x.Nombre,
                  edo = x.IdEstado
              })
              .Where(x =>
                SqlExpr.EqualsNullable.Invoke(x.IdRegistro, idRegistro) &&
                SqlExpr.EqualsNullable.Invoke(x.IdEstado, idEstado)
            )
              ;

            var actual = r.ToSql(SqlText.ParamMode.Substitute).Sql;
            var expected = @"
SELECT 
    ""x"".""Nombre"" AS ""nom"", 
    ""x"".""IdEstado"" AS ""edo""
FROM ""Cliente"" ""x""
WHERE (""x"".""IdEstado"" = 123)
";
            AssertSql.AreEqual(expected, actual);
        }



        [TestMethod]
        public void CastCSharp()
        {
            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  test = (int)Sql.Cast(Sql.Extract(Sql.ExtractField.Month, x.Fecha), SqlType.Int)
              });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    CAST (EXTRACT(MONTH FROM ""x"".""Fecha"") AS int) AS ""test""
FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);
        }


        [TestMethod]
        public void SimpleExtract()
        {
            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  mes = Sql.Extract(Sql.ExtractField.Day, x.Fecha)
              });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    EXTRACT(DAY FROM ""x"".""Fecha"") AS ""mes""
FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SimpleInterval()
        {
            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  mes = Sql.Interval(4, Sql.IntervalUnit.Days)
              });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    (4 * interval '1 days') AS ""mes""
FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);
        }


        [TestMethod]
        public void SimpleSelectIn()
        {
            var nombres = new[]
            {
                "hola",
                "rafa"
            };

            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  esRafa = nombres.Contains(x.Nombre)
              });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT (""x"".""Nombre"" IN ('hola','rafa')) AS ""esRafa"" FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SimpleSelectOrderByDesc()
        {
            var r =
Sql
.From(new SqlTable<Cliente>())
.Select(x => new
{
    nom = x.Nombre,
    edo = x.IdEstado
})
.OrderBy(x => x.Nombre, OrderByOrder.Desc)
;

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    ""x"".""Nombre"" AS ""nom"", 
    ""x"".""IdEstado"" AS ""edo""
FROM ""Cliente"" ""x""
ORDER BY ""x"".""Nombre"" DESC
";
            AssertSql.AreEqual(expected, actual);
        }


        [TestMethod]
        public void SimpleSelectMultiWhere()
        {
            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  nom = x.Nombre,
                  edo = x.IdEstado
              })
              .Where(x => x.Nombre == "Rafa" && x.IdEstado == 2)
              ;

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    ""x"".""Nombre"" AS ""nom"", 
    ""x"".""IdEstado"" AS ""edo""
FROM ""Cliente"" ""x""
WHERE ((""x"".""Nombre"" = 'Rafa') AND (""x"".""IdEstado"" = 2))
";
            AssertSql.AreEqual(expected, actual);
        }


        [TestMethod]
        public void SimpleSelectLimit()
        {
            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  nom = x.Nombre,
                  edo = x.IdEstado
              })
              .Limit(1)
              ;

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    ""x"".""Nombre"" AS ""nom"", 
    ""x"".""IdEstado"" AS ""edo""
FROM ""Cliente"" ""x""
LIMIT 1
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void StringConcat()
        {
            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  nom = x.Nombre + " " + x.Nombre,
              });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    ((""x"".""Nombre"" || ' ') || ""x"".""Nombre"") AS ""nom""
FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);

        }

        [TestMethod]
        public void StringLike()
        {
            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  rafa = x.Nombre.Contains("Rafa"),
              });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    (""x"".""Nombre"" LIKE '%' || 'Rafa' || '%') AS ""rafa""
FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);

        }

        [TestMethod]
        public void StringLen()
        {
            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  rafa = x.Nombre.Length,
              });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    char_length(""x"".""Nombre"") AS ""rafa""
FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);

        }

        [TestMethod]
        public void SelectReadComplexTypes()
        {
            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  tel = x.Dir.Personales.Telefono
              });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    ""x"".""Dir_Personales_Telefono"" AS ""tel""
FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectReadComplexTypes2()
        {

            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  tel = x.Dir
              });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    ""x"".""Dir_Calle"" AS ""tel_Calle"",
    ""x"".""Dir_Personales_Telefono"" AS ""tel_Personales_Telefono""
FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectReadComplexTypes3()
        {
            var r = Sql
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  personales = x.Dir.Personales
              });

            var clause = r.Clause;
            var actual = SqlText.SqlSelect.SelectToStringSP(clause);
            var expected = @"
SELECT 
    ""x"".""Dir_Personales_Telefono"" AS ""personales_Telefono""
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

        public void JoinSyntaxTest()
        {
            var r = Sql
                .From<Cliente>()
                .Join<Estado>().On(x => x.Item1.IdEstado == x.Item2.IdRegistro)
                .Join<Factura>().On(x => x.Item3.IdCliente == x.Item1.IdRegistro)
                .Alias(x => new
                {
                    cli = x.Item1,
                    est = x.Item2,
                    fac = x.Item3
                })
                ;
        }

        [TestMethod]
        public void SimpleJoinSelect()
        {
            var r = Sql
              .From<Cliente>()
              .Join<Estado>()
              .On(x => x.Item1.IdEstado == x.Item2.IdRegistro)
              .Alias(x => new
              {
                  cli = x.Item1,
                  edo = x.Item2,
              })
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
        public void SqlSelectComplexType()
        {
            ISqlSelect query = Sql.From<Cliente>().Select(x => new Cliente
            {
                Nombre = "Hola",
                Apellido = x.Apellido,
                Dir = new Direccion
                {
                    Calle = x.Dir.Calle,
                    Personales = new DatosPersonales
                    {
                        Telefono = "1234"
                    }
                }
            });

            var r = query.ToSql().Sql;
            var expected = @"
SELECT 
    'Hola' AS ""Nombre"", 
    ""x"".""Apellido"" AS ""Apellido"", 
    ""x"".""Dir_Calle"" AS ""Dir_Calle"",
    '1234' AS ""Dir_Personales_Telefono""
FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, r);
        }

        [TestMethod]
        public void SqlMultiStar()
        {
            var r = Sql
                .From<Cliente>()
                .Join<Estado>()
                .On(x => x.Item1.IdEstado == x.Item2.IdRegistro)
                .Alias(x => new
                {
                    cli = x.Item1,
                    edo = x.Item2,
                })
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
                    .From<Cliente>()
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
                 .From<Cliente>()
                 .Join<Estado>()
                 .On(x => x.Item1.IdEstado == x.Item2.IdRegistro)
                 .Alias(x => new
                 {
                     cli = x.Item1,
                     edo = x.Item2,
                 })
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
                 .From<Cliente>()
                 .Join<Estado>()
                 .On(x => x.Item1.IdEstado == x.Item2.IdRegistro)
                 .Alias(x => new
                 {
                     cli = x.Item1,
                     edo = x.Item2,
                 })
                 .Select(x => new
                 {
                     cliId = x.cli.IdRegistro,
                     edoId = x.edo.IdRegistro
                 })
            )
            .Join<Factura>()
            .On(x => x.Item2.IdCliente == x.Item1.cliId)
            .Alias(x => new
            {
                sq = x.Item1,
                fac = x.Item2,
            })
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
