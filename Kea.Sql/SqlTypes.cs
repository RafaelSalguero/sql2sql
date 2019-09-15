using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql
{
    /// <summary>
    /// Un tipo de postgre
    /// </summary>
    public class SqlType
    {
        public SqlType(string sql)
        {
            Sql = sql;
        }

        public string Sql { get; }

        public static SqlType Bool => new SqlType("bool");
        static SqlType CharLike(string pre, int? n = null, string post = null) => new SqlType(pre + (n == null ? "" : $" ({n})") + (post != null ? " " + post : ""));

        public static SqlType Char(int? n = null) => CharLike("char", n);
        public static SqlType VarChar(int? n = null) => CharLike("varchar", n);

        public static SqlType Double => new SqlType("float8");
        public static SqlType Real => new SqlType("float4");
        public static SqlType Int => new SqlType("int");
        public static SqlType Serial => new SqlType("serial");
        public static SqlType Text => new SqlType("text");

        public static SqlType Time(int? p = null) => CharLike("time", p);
        public static SqlType TimeTZ(int? p = null) => CharLike("time", p, "with time zone");
        public static SqlType TimeStamp(int? p = null) => CharLike("time", p);
        public static SqlType TimeStampTZ(int? p = null) => CharLike("time", p, "with time zone");

        public static SqlType Uuid => new SqlType("uuid");

        public static SqlType Numeric(int precision, int scale) => new SqlType($"numeric({precision}, {scale})");
        public static SqlType Numeric(int precision) => new SqlType($"numeric({precision})");
        public static SqlType Numeric() => new SqlType($"numeric");
    }

     
}
