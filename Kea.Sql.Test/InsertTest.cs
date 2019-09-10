using KeaSql.Fluent.Data;
using KeaSql.SqlText;
using KeaSql.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Test
{ 
    [TestClass]
   public class InsertTest
    {
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

            var ret = SqlInsert.InsertToString(clause, ParamMode.Substitute, new SqlParamDic());
            var expected = @"
INSERT INTO ""Cliente"" (""Nombre"", ""Apellido"")
VALUES ('Rafael', 'Salguero')
";

            AssertSql.AreEqual(expected, ret);
        }

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

            var ret = SqlInsert.InsertToString(clause, ParamMode.Substitute, new SqlParamDic());
            var expected = @"
INSERT INTO ""Cliente"" (""Nombre"", ""Apellido"", ""Dir_Calle"", ""Dir_Personales_Telefono"")
VALUES ('Rafael', 'Salguero', 'E Baca Calderon', '4123')
";

            AssertSql.AreEqual(expected, ret);
        }

        [TestMethod]
        public void QueryInsertTest()
        {
            var query = Sql.FromTable<Cliente>().Select(x => new Cliente
            {
                 Nombre = "Hola",
                 Apellido = x.Apellido,
                 Dir = new Direccion
                 {
                      Calle= x.Dir.Calle
                 }
            });


            var clause = new InsertClause(
                 table: "Cliente",
                 value: null,
                 query: query.Clause,
                 onConflict: null,
                 returning: null
             );

            var ret = SqlInsert.InsertToString(clause, ParamMode.Substitute, new SqlParamDic());
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
    }
}
