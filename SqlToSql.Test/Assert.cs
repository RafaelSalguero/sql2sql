using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KeaSql.Test
{
    public static  class AssertSql
    {
        public  static string NormalizeSql(string x)
        {
            var ret = x;

            ret = new Regex(@"(\r|\n|\s|\t)+").Replace(ret, " ").Trim();
            ret = new Regex(@"\( ").Replace(ret, "(");
            ret = new Regex(@" \)").Replace(ret, ")");
            return ret;
        }
        public static void AreEqual(string expected, string actual)
        {
            var aN = NormalizeSql(expected);
            var bN = NormalizeSql(actual);
            Assert.AreEqual(aN, bN);
        }
    }
}
