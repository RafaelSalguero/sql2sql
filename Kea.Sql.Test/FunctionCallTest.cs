using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeaSql.Fluent;
using KeaSql.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KeaSql.Test
{
    [TestClass]
    public class FunctionCallTest
    {
        [TestMethod]
        public void WithCall()
        {
            var q =
                Sql
                .With(Sql.FromTable<Cliente>().Select(x => x))
                .With(w => QueryClientes(w))
                .Map((a, b) => new
                {
                    cli1 = a,
                    cli2 = b
                })
                .With(x => QueryClientes(x.cli2))
                .Map((a,b) => new
                {
                    cli2 = a.cli2,
                    cli3 = b
                })
                .Query(w =>
                    Sql.From(w.cli3).Select(x => x)
                );

            var sql = q.ToSql();
        }

        public ISqlSelect<Cliente> QueryClientes(IFromListItemTarget<Cliente> clientes)
        {
            return Sql.From(clientes).Select(x => x);
        }
    }
}
