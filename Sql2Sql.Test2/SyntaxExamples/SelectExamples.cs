using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sql2Sql.Test.SyntaxExamples
{
    [TestClass]
    public class SelectExamples
    {
        [TestMethod]
        public void SelectAllFromTable()
        {
            var qs = new ISqlSelect<Customer>[] {
                Sql.From<Customer>(),
                Sql.From<Customer>().Select(x => x),
            };

            foreach (var q in qs)
            {
                var actual = q.ToString();
                var expected = @"
SELECT ""x"".* FROM ""Customer"" ""x""
";

                AssertSql.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void Distinct()
        {
            var q = Sql.From<Customer>().Distinct();
            var actual = q.ToString();
            var expected = @"
SELECT DISTINCT ""x"".* FROM ""Customer"" ""x""
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DistinctOn()
        {
            var q = Sql
                .From<Customer>()
                .DistinctOn(x => x.LocationId)
                .Select(x => x)
                ;
            var actual = q.ToString();
            var expected = @"
SELECT 
    DISTINCT ON (""x"".""LocationId"") 
    ""x"".* 
FROM ""Customer"" ""x""
";

            AssertSql.AreEqual(expected, actual);
        }
    }
}
