using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlToSql.Fluent;

namespace SqlToSql.Test
{
    [TestClass]
    public class UnitTest1
    {


        [TestMethod]
        public void TestMethod1()
        {
            
            Expression<Func<int, bool>> test  = x=> x == 2;

            var r = Sql2
               .From(new SqlTable<Cliente>())
               .Join(new SqlTable<Estado>()).On((a, b) => new
               {
                   cli = a,
                   edo = b
               }, x => x.cli.IdEstado == x.edo.IdRegistro)
               .Join(new SqlTable<Factura>()).On((a, b) => new
               {
                   cliente = a.cli,
                   estado = a.edo,
                   factura = b
               }, x => x.cliente.IdRegistro == x.factura.IdCliente)
               .Join(new SqlTable<ConceptoFactura>()).On((a, b) => new
               {
                   clien = a.cliente,
                   fact = a.factura,
                   concepto = b,
                   esta = a.estado
               }, x => x.concepto.IdFactura == x.fact.IdRegistro)
               ;
            /*
             FROM "Cliente" clien
             JOIN "Estado" estado ON clien."IdEstado" = estado."IdRegistro"
             JOIN "Factura" fact ON clien."IdRegistro" = fact."IdCliente",
             JOIN "ConceptoFactura" concepto ON concepto."IdFactura" = fact."IdRegistro"
             */

            dynamic from = r.Clause.From;
            var str = SqlText.JoinToStr(from);
        }
    }
}
