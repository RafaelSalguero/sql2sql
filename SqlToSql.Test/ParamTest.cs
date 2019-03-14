using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlToSql.Fluent;

namespace SqlToSql.Test
{
    [TestClass]
    public class ParamTest
    {
        [TestMethod]
        public void Param()
        {
            var id = 10;
            var q = Sql.From<Cliente>()
                .Select(x => x)
                .Where(x => x.IdRegistro == id)
                .ToSql();
        }
    }
}
