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

        [TestMethod]
        public void Star()
        {
            var q = Sql
                .From<Customer>()
                .Select(x => Sql.Star().Map(new
                {
                    FullName = x.Name + x.LastName
                }));

            var actual = q.ToString();
            var expected = @"
SELECT *, (""x"".""Name"" || ""x"".""LastName"") AS ""FullName""
FROM ""Customer"" ""x""
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Where()
        {
            var q = Sql
                .From<Customer>()
                .Where(x => x.LastName == "Kahlo");

            var actual = q.ToString();
            var expected = @"
SELECT ""x"".*
FROM ""Customer"" ""x""
WHERE (""x"".""LastName"" = 'Kahlo')
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GroupBy()
        {
            var q = Sql
                .From<Customer>()
                .Select(x => x)
                .GroupBy(x => x.Name).ThenBy(x => x.LastName)
                ;
            var actual = q.ToString();
            var expected = @"
SELECT ""x"".*
FROM ""Customer"" ""x""
GROUP BY ""x"".""Name"", ""x"".""LastName""
";

            AssertSql.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Limit()
        {
            var q = Sql
                .From<Customer>()
                .Limit(100)
                ;
            var actual = q.ToString();
            var expected = @"
SELECT ""x"".*
FROM ""Customer"" ""x""
LIMIT 100
";

            AssertSql.AreEqual(expected, actual);
        }
    }
}
