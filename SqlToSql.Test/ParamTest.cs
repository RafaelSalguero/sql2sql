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

            var expected = @"
SELECT 
    ""x"".*
FROM ""Cliente"" ""x""
WHERE (""x"".""IdRegistro"" = @id)
";
            AssertSql.AreEqual(expected, q.Sql);
            Assert.AreEqual(q.Params[0].Name, "id");
            Assert.AreEqual(q.Params[0].Value, 10);
        }

        public class ParA
        {
            public int Param { get; set; }
        }
        public class ParB
        {
            public ParA ParA { get; set; }
        }

        [TestMethod]
        public void ParamClass()
        {
            var pars = new ParB
            {
                ParA = new ParA
                {
                    Param = 20
                }
            };

            var q = Sql.From<Cliente>()
                .Select(x => x)
                .Where(x => x.IdRegistro == pars.ParA.Param)
                .ToSql();

            var expected = @"
SELECT 
    ""x"".*
FROM ""Cliente"" ""x""
WHERE (""x"".""IdRegistro"" = @Param)
";
            AssertSql.AreEqual(expected, q.Sql);
            Assert.AreEqual(q.Params[0].Name, "Param");
            Assert.AreEqual(q.Params[0].Value, 20);
        }
    }
}
