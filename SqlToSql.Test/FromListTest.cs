﻿using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlToSql.Fluent;
using SqlToSql.SqlText;

namespace SqlToSql.Test
{
    [TestClass]
    public class FromListTest
    {
       

        [TestMethod]
        public void SimpleJoin()
        {
            var r = Sql2
           .From(new SqlTable<Cliente>())
           .Join(new SqlTable<Estado>()).On((a, b) => new
           {
               cli = a,
               edo = b
           }, x => x.cli.IdEstado == x.edo.IdRegistro)
           ;
            var actual = SqlFromList.FromListToStr(r.Clause.From);
            var expected = @"
FROM ""Cliente"" cli
JOIN ""Estado"" edo ON (cli.""IdEstado"" = edo.""IdRegistro"")
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MultiJoin()
        {
            Expression<Func<int, bool>> test = x => x == 2;
            var r = Sql2
               .From(new SqlTable<Cliente>())
               .Join(new SqlTable<Estado>()).On((a, b) => new
               {
                   cli = a,
                   edo = b
               }, x => x.cli.IdEstado == x.edo.IdRegistro)
               .Join(new SqlTable<Factura>()).On((c, d) => new
               {
                   cliente = c.cli,
                   estado = c.edo,
                   factura = d
               }, y => y.cliente.IdRegistro == y.factura.IdCliente)
               .Join(new SqlTable<ConceptoFactura>()).On((e, f) => new
               {
                   clien = e.cliente,
                   fact = e.factura,
                   concepto = f
               }, z => z.concepto.IdFactura == z.fact.IdRegistro)
               ;
            var actual = SqlFromList.FromListToStr(r.Clause.From);

            var expected = @"
FROM ""Cliente"" clien
JOIN ""Estado"" estado ON (clien.""IdEstado"" = estado.""IdRegistro"")
JOIN ""Factura"" fact ON (clien.""IdRegistro"" = fact.""IdCliente"")
JOIN ""ConceptoFactura"" concepto ON (concepto.""IdFactura"" = fact.""IdRegistro"")
";
            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void NameCollisionJoin()
        {
            var r = Sql2
                .From(new SqlTable<Cliente>())
                .Join(new SqlTable<Estado>()).On((a, b) => new
                {
                    a = a,
                    b = b
                }, x => x.a.IdEstado == x.b.IdRegistro)
                .Join(new SqlTable<Factura>()).On((a, b) => new
                {
                    a = a.b,
                    b = b
                }, x => x.a.IdRegistro == x.b.IdRegistro)
                .Join(new SqlTable<ConceptoFactura>()).On((a, b) => new
                {
                    a = a.b,
                    b = b
                }, x => x.a.IdCliente == x.b.IdFactura);
            ;



            var expected = @"
FROM ""Cliente"" a2
JOIN ""Estado"" a1 ON (a2.""IdEstado"" = a1.""IdRegistro"")
JOIN ""Factura"" a ON (a1.""IdRegistro"" = a.""IdRegistro"")
JOIN ""ConceptoFactura"" b ON (a.""IdCliente"" = b.""IdFactura"")
";

            var actual = SqlFromList.FromListToStr(r.Clause.From);
            AssertSql.AreEqual(expected, actual);
        }
    }
}
