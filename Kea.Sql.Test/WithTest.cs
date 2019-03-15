using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeaSql;
using KeaSql.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KeaSql.Test
{
    [TestClass]
    public class WithTest
    {
        [TestMethod]
        public void WithSyntax()
        {
            Sql.With(
                Sql.From<Cliente>()
                .Select(x => x)
            ).With(cli =>
                Sql
                .From<Factura>()
                .Inner().Join(cli).OnTuple(x => x.Item1.IdCliente == x.Item2.IdRegistro)
                .Select(x => new
                {
                    x.Item1.IdCliente,
                    x.Item2.Nombre,
                })
            ).Map((a, b) => new
            {
                cliente = a,
                facturas = b
            })
            .WithRecursive(w =>
                Sql
                .From<ConceptoFactura>()
                .Inner().Join(w.facturas).OnTuple(x => x.Item1.IdFactura == x.Item2.IdCliente)
                .Select(x => x)
            ).UnionAll((w, conceptos) =>
                Sql.From(conceptos)
                .Select(x => x)
            );
        }
    }
}
