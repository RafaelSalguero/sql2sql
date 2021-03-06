﻿using Sql2Sql.Fluent.Data;
using Sql2Sql.SqlText;
using Sql2Sql.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Sql2Sql.SqlText.Insert;

namespace Sql2Sql.Test
{
    [TestClass]
    public class InsertClauseTest
    {
        /// <summary>
        /// Prueba un insert con un value expression simple
        /// </summary>
        [TestMethod]
        public void SimpleInsertTest()
        {
            Expression<Func<Cliente>> valueExpr = () => new Cliente
            {
                Nombre = "Rafael",
                Apellido = "Salguero",
            };

            var clause = new InsertClause(
                table: "Cliente",
                value: valueExpr.Body,
                query: null,
                onConflict: null,
                returning: null
                );

            var ret = SqlInsertConverter.InsertToString(clause, ParamMode.Substitute, new SqlParamDic()).Sql;
            var expected = @"
INSERT INTO ""Cliente"" (""Nombre"", ""Apellido"")
VALUES ('Rafael', 'Salguero')
";

            AssertSql.AreEqual(expected, ret);
        }

        /// <summary>
        /// Insert con complex types
        /// </summary>
        [TestMethod]
        public void ComplexTypeInsertTest()
        {
            Expression<Func<Cliente>> valueExpr = () => new Cliente
            {
                Nombre = "Rafael",
                Apellido = "Salguero",
                Dir = new Direccion
                {
                    Calle = "E Baca Calderon",
                    Personales = new DatosPersonales
                    {
                        Telefono = "4123"
                    }
                }
            };

            var clause = new InsertClause(
                table: "Cliente",
                value: valueExpr.Body,
                query: null,
                onConflict: null,
                returning: null
                );

            var ret = SqlInsertConverter.InsertToString(clause, ParamMode.Substitute, new SqlParamDic()).Sql;
            var expected = @"
INSERT INTO ""Cliente"" (""Nombre"", ""Apellido"", ""Dir_Calle"", ""Dir_Personales_Telefono"")
VALUES ('Rafael', 'Salguero', 'E Baca Calderon', '4123')
";

            AssertSql.AreEqual(expected, ret);
        }

        /// <summary>
        /// Insert de un query
        /// </summary>
        [TestMethod]
        public void QueryInsertTest()
        {
            var query = Sql.From<Cliente>().Select(x => new Cliente
            {
                Nombre = "Hola",
                Apellido = x.Apellido,
                Dir = new Direccion
                {
                    Calle = x.Dir.Calle
                }
            });


            var clause = new InsertClause(
                 table: "Cliente",
                 value: null,
                 query: query.Clause,
                 onConflict: null,
                 returning: null
             );

            var ret = SqlInsertConverter.InsertToString(clause, ParamMode.Substitute, new SqlParamDic()).Sql;
            var expected = @"
INSERT INTO ""Cliente"" (Nombre, Apellido, Dir_Calle)
SELECT 
    'Hola' AS ""Nombre"", 
    ""x"".""Apellido"" AS ""Apellido"", 
    ""x"".""Dir_Calle"" AS ""Dir_Calle""
FROM ""Cliente"" ""x""
";
            AssertSql.AreEqual(expected, ret);
        }

        /// <summary>
        /// Insert con un ON CONFLICT
        /// </summary>
        [TestMethod]
        public void SimpleOnConflict()
        {
            Expression<Func<Cliente>> valueExpr = () => new Cliente
            {
                Nombre = "Rafael",
                Apellido = "Salguero",
            };

            Expression<Func<Cliente, int>> indexExpr = x => x.IdRegistro;

            Expression<Func<Cliente, Cliente, Cliente>> updateExpr = (excluded, orig) => new Cliente
            {
                Nombre = excluded.Nombre + orig.Nombre,
                Apellido = orig.Apellido,
                Tipo = TipoPersona.Fisica
            };

            var doUpdate = new OnConflictDoUpdateClause(
                set: updateExpr,
                where: null
                );

            var onConf = new OnConflictClause(
                indexExpressions: new LambdaExpression[] { indexExpr },
                where: null,
                doUpdate: doUpdate
                );

            var clause = new InsertClause(
                table: "Cliente",
                value: valueExpr.Body,
                query: null,
                onConflict: onConf,
                returning: null
                );

            var ret = SqlInsertConverter.InsertToString(clause, ParamMode.Substitute, new SqlParamDic()).Sql;
            var expected = @"
INSERT INTO ""Cliente"" (""Nombre"", ""Apellido"")
VALUES ('Rafael', 'Salguero')
ON CONFLICT (""IdRegistro"") DO UPDATE
SET
    ""Nombre"" = (EXCLUDED.""Nombre"" || ""Cliente"".""Nombre""), 
    ""Apellido"" = ""Cliente"".""Apellido"", 
    ""Tipo"" = 0
";

            AssertSql.AreEqual(expected, ret);
        }

        class ReturningTestType
        {
            public int id { get; set; }
        }

        /// <summary>
        /// Prueba la cláusula RETURNING del INSERT
        /// </summary>
        [TestMethod]
        public void SimpleReturning()
        {
            Expression<Func<Cliente>> valueExpr = () => new Cliente
            {
                Nombre = "Rafael",
                Apellido = "Salguero",
            };

            Expression<Func<Cliente, ReturningTestType>> returningExpr = x => new ReturningTestType
            {
                id = x.IdRegistro
            };

            var clause = new InsertClause(
                table: "Cliente",
                value: valueExpr.Body,
                query: null,
                onConflict: null,
                returning: returningExpr
                );

            var ret = SqlInsertConverter.InsertToString(clause, ParamMode.Substitute, new SqlParamDic()).Sql;
            var expected = @"
INSERT INTO ""Cliente"" (""Nombre"", ""Apellido"")
VALUES ('Rafael', 'Salguero')
RETURNING 
    ""Cliente"".""IdRegistro"" AS ""id""
";

            AssertSql.AreEqual(expected, ret);
        }
    }
}
