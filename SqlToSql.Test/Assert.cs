using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SqlToSql.Test
{
    public static  class AssertSql
    {
        public  static string NormalizeSql(string x)
        {
            return new Regex(@"(\r|\n|\s|\t)+").Replace(x, " ").Trim();
        }
        public static void AreEqual(string expected, string actual)
        {
            var aN = NormalizeSql(expected);
            var bN = NormalizeSql(actual);
            Assert.AreEqual(aN, bN);
        }
    }
}
