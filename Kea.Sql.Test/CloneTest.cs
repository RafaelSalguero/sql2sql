using KeaSql.Tests;
using LinqKit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KeaSql.Test
{
    [TestClass]
    public class CloneTest
    {
        public class FacturaDto : Factura
        {
            public string NombreCliente { get; set; }
        }


        [TestMethod]
        public void CloneSimpleTest()
        {
            var obtenerDto =
                Tonic.LinqEx.CloneSimple<Factura, Cliente, FacturaDto>((fac, cli) => new FacturaDto
                {
                    NombreCliente = cli.Nombre
                });

            var r = Sql
                    .FromTable<Factura>()
                    .Inner().JoinTable<Cliente>()
                    .OnTuple(x => x.Item1.IdCliente == x.Item2.IdRegistro)
                    .Alias(x => new
                    {
                        fac = x.Item1,
                        cli = x.Item2
                    })
                    .Select(from => obtenerDto.Invoke(from.fac, from.cli)
                    );

            var actual = r.ToSql().Sql;
            var expected = @"
SELECT 
    ""fac"".""IdRegistro"" AS ""IdRegistro"", 
    ""fac"".""IdCliente"" AS ""IdCliente"", 
    ""fac"".""Folio"" AS ""Folio"", 
    ""fac"".""Serie"" AS ""Serie"", 
    ""cli"".""Nombre"" AS ""NombreCliente""
FROM ""Factura"" ""fac""
JOIN ""Cliente"" ""cli"" ON (""fac"".""IdCliente"" = ""cli"".""IdRegistro"")
";

            AssertSql.AreEqual(expected, actual);
        }
        [TestMethod]
        public void CloneSimpleTest2()
        {
            var r = Sql
                 .FromTable<Factura>()
                 .Inner().JoinTable<Cliente>()
                 .OnTuple(x => x.Item1.IdCliente == x.Item2.IdRegistro)
                 .Alias(x => new
                 {
                     fac = x.Item1,
                     cli = x.Item2
                 })
                 .Select(from => Tonic.LinqEx.CloneSimpleSelector(from, x => x.fac, x => new FacturaDto
                 {
                     NombreCliente = x.cli.Nombre
                 }).Invoke(from)
                 );

            var actual = r.ToSql().Sql;
            var expected = @"
SELECT 
    ""fac"".""IdRegistro"" AS ""IdRegistro"", 
    ""fac"".""IdCliente"" AS ""IdCliente"", 
    ""fac"".""Folio"" AS ""Folio"", 
    ""fac"".""Serie"" AS ""Serie"", 
    ""cli"".""Nombre"" AS ""NombreCliente""
FROM ""Factura"" ""fac""
JOIN ""Cliente"" ""cli"" ON (""fac"".""IdCliente"" = ""cli"".""IdRegistro"")
";

            AssertSql.AreEqual(expected, actual);
        }
    }
}
