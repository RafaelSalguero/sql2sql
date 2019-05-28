using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Test
{
    [TestClass]
    public class SqlExprTest
    {
        [TestMethod]
        public void RangeTest()
        {
            var range = SqlExpr.Range<DateTime>();
            var comp = range.Compile();

            Assert.IsTrue(comp(new DateTime(2019, 01, 26), new DateTime(2019, 01, 30), new DateTime(2019, 01, 28)));
            Assert.IsFalse(comp(new DateTime(2019, 01, 26), new DateTime(2019, 01, 30), new DateTime(2019, 01, 31)));
            Assert.IsTrue(comp(new DateTime(2019, 01, 26), null, new DateTime(2019, 01, 31)));
        }
    }
}
