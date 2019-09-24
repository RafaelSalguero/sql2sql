using Sql2Sql.Tests;
using LinqKit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq.Expressions;
using System;

namespace Sql2Sql.Test
{
    [TestClass]
    public class ExpressionBasedFunctionTest
    {


        /// <summary>
        /// A dummy function that simulates an operation on a given expression
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        Expression<Func<Factura, Factura>> ExprFunc(Expression<Func<Factura, Factura>> expr)
        {
            return fac => new Factura
            {
                Folio = 42,
                Serie = "Rafa"
            };
        }

        /// <summary>
        /// Tests using a function that operates over expression trees inside the SELECT clause
        /// </summary>
        [TestMethod]
        public void ExpressionTest()
        {
            var obtenerDto =
                ExprFunc(fac => new Factura
                {
                    IdCliente = 10
                }); ;

            var r = Sql
                    .FromTable<Factura>()
                    .Inner().JoinTable<Cliente>()
                    .OnTuple(x => x.Item1.IdCliente == x.Item2.IdRegistro)
                    .Alias(x => new
                    {
                        fac = x.Item1,
                        cli = x.Item2
                    })
                    .Select(from => obtenerDto.Invoke(from.fac)
                    );

            var actual = r.ToSql().Sql;
            var expected = @"
SELECT 
    42 AS ""Folio"", 
    'Rafa' AS ""Serie""
FROM ""Factura"" ""fac""
JOIN ""Cliente"" ""cli"" ON (""fac"".""IdCliente"" = ""cli"".""IdRegistro"")
";

            AssertSql.AreEqual(expected, actual);
        }
    }
      
}
