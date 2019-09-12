using KeaSql.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Test.Inserts
{
    [TestClass]
    public class InsertTest
    {
        [TestMethod]
        public void SyntaxDoNothing()
        {
            var sql = Sql
                .InsertInto<Cliente>()
                .Values(() => new Cliente
                {
                    Nombre = "Rafael",
                    Apellido = "Salguero"
                })
                .OnConflict()
                .DoNothing()
                .ToSql()
                ;

            var expected = @"
INSERT INTO ""Cliente"" (""Nombre"", ""Apellido"")
VALUES ('Rafael', 'Salguero')
ON CONFLICT DO NOTHING
";

            AssertSql.AreEqual(sql.Sql, expected);
        }

        [TestMethod]
        public void SyntaxDoUpdate()
        {
            var sql = Sql
                .InsertInto<Cliente>()
                .Values(() => new Cliente
                {
                    Nombre = "Rafael",
                    Apellido = "Salguero"
                     
                })
                .OnConflict(x => x.IdRegistro)
                .DoUpdate((exc) => new Cliente
                {
                     Precio = exc.Precio + 1
                })
                .ToSql()
                ;

            var expected = @"
INSERT INTO ""Cliente"" (""Nombre"", ""Apellido"")
VALUES ('Rafael', 'Salguero')
ON CONFLICT (""IdRegistro"") DO UPDATE
SET
    ""Precio"" = (EXCLUDED.""Precio"" + 1)
";

            AssertSql.AreEqual(sql.Sql, expected);
        }

        [TestMethod]
        public void SyntaxReturning()
        {
            var q = Sql
                .InsertInto<Cliente>()
                .Values(() => new Cliente
                {
                    Nombre = "Rafael",
                    Apellido = "Salguero"

                })
                .OnConflict(x => x.IdRegistro)
                .DoUpdate((exc) => new Cliente
                {
                    Precio = exc.Precio + 1
                })
                .Returning(x => new
                {
                     id = x.IdRegistro
                })
                ;

            var sql = q.ToSql();

            var expected = @"
INSERT INTO ""Cliente"" (""Nombre"", ""Apellido"")
VALUES ('Rafael', 'Salguero')
ON CONFLICT (""IdRegistro"") DO UPDATE
SET
    ""Precio"" = (EXCLUDED.""Precio"" + 1)
RETURNING 
    ""Cliente"".""IdRegistro"" AS ""id""
";

            AssertSql.AreEqual(sql.Sql, expected);
        }

        [TestMethod]
        public void InsertFromLinq()
        {
            var data = new[]
            {
                new Cliente
                {
                    Nombre = "Rafa"
                },
                new Cliente
                {
                    Nombre = "Ale"
                }
            };

            var sts = data.Select(x => Sql
            .InsertInto<Cliente>()
            .Values(() => new Cliente
            {
                Nombre = x.Nombre,
            }));

            var ret = sts.Select(x => x.ToSql());
        }

    }
}
