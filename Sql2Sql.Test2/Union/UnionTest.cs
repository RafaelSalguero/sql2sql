using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sql2Sql.Tests;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sql2Sql.Test.Union
{
    [TestClass]
    public class UnionTest
    {
        [TestMethod]
        public void MultipleUnionAllTest()
        {
            var query =
                Sql.From(
                    Sql.From<Pago>()
                    .Select(x => new
                    {
                        Id = x.IdRegistro,
                        Cliente = x.IdCliente
                    })
                    .UnionAll(
                        Sql.From<Factura>()
                        .Select(x => new
                        {
                            Id = x.IdRegistro,
                            Cliente = x.IdCliente
                        })
                    )
                    .UnionAll(
                        Sql.From<Cliente>()
                        .Select(x => new
                        {
                            Id = x.IdRegistro,
                            Cliente = x.IdEstado
                        })
                    )
                )
                .OrderBy(x => x.Cliente)
                ;

            var actual = query.ToString();
            var expected = @"
SELECT
    ""x"".*
FROM (
    (
        SELECT
            ""x"".""IdRegistro"" AS ""Id"", 
            ""x"".""IdCliente"" AS ""Cliente""
        FROM ""Pago"" ""x""
        
    )
    UNION ALL
    (
        SELECT
            ""x"".""IdRegistro"" AS ""Id"", 
            ""x"".""IdCliente"" AS ""Cliente""
        FROM ""Factura"" ""x""
    )
    UNION ALL
    (
        SELECT
            ""x"".""IdRegistro"" AS ""Id"", 
            ""x"".""IdEstado"" AS ""Cliente""
        FROM ""Cliente"" ""x""
    )
) ""x""
ORDER BY ""x"".""Cliente"" ASC
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void UnionAllOrderBy()
        {
            var query =
                Sql.From<Pago>()
                .Select(x => new
                {
                    Id = x.IdRegistro,
                    Cliente = x.IdCliente
                })
                .OrderBy(x => x.IdCliente)
                .UnionAll(
                    Sql.From<Factura>()
                    .Select(x => new
                    {
                        Id = x.IdRegistro,
                        Cliente = x.IdCliente
                    })
                    .OrderBy(x => x.IdRegistro)
                )
                ;
            
            var actual = query.ToString();
            var expected = @"
(
    SELECT
        ""x"".""IdRegistro"" AS ""Id"", 
        ""x"".""IdCliente"" AS ""Cliente""
    FROM ""Pago"" ""x""
    ORDER BY ""x"".""IdCliente"" ASC
)
UNION ALL
(
    SELECT
        ""x"".""IdRegistro"" AS ""Id"", 
        ""x"".""IdCliente"" AS ""Cliente""
    FROM ""Factura"" ""x""
    ORDER BY ""x"".""IdRegistro"" ASC
)
";
            
            AssertSql.AreEqual(expected, actual);

        }
    }
}
