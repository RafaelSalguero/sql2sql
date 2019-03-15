using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeaSql.Fluent;

namespace KeaSql
{
    /// <summary>
    /// Funciones de SQL
    /// </summary>
    public static partial class Sql
    {

        [SqlName("count")]
        public static int Count<T>(T expr) => throw new SqlFunctionException();
        [SqlName("sum")]
        public static T Sum<T>(T expr) => throw new SqlFunctionException();
        [SqlName("max")]
        public static T Max<T>(T expr) => throw new SqlFunctionException();
        [SqlName("min")]
        public static T Min<T>(T expr) => throw new SqlFunctionException();

        [SqlName("round")]
        public static T Round<T>(T value, int places = 0) => throw new SqlFunctionException();

        [SqlName("coalesce")]
        public static T Coalesce<T>(T? a, T b) where T : struct => throw new SqlFunctionException();
        [SqlName("coalesce")]
        public static T Coalesce<T>(T? a, T? b, T c) where T : struct => throw new SqlFunctionException();
        [SqlName("coalesce")]
        public static T Coalesce<T>(T a, T b) => throw new SqlFunctionException();
        [SqlName("coalesce")]
        public static T Coalesce<T>(T a, T b, T c) => throw new SqlFunctionException();

        [SqlName("greatest")]
        public static T Greatest<T>(T a, T b) => throw new SqlFunctionException();
        [SqlName("greatest")]
        public static T Greatest<T>(T a, T b, T c) => throw new SqlFunctionException();
        [SqlName("greatest")]
        public static T Greatest<T>(T a, T b, T c, T d) => throw new SqlFunctionException();

        [SqlName("least")]
        public static T Least<T>(T a, T b) => throw new SqlFunctionException();
        [SqlName("least")]
        public static T Least<T>(T a, T b, T c) => throw new SqlFunctionException();
        [SqlName("least")]
        public static T Least<T>(T a, T b, T c, T d) => throw new SqlFunctionException();

        public enum DateTruncField
        {
            [SqlName("'microseconds'")]
            Microseconds,
            [SqlName("'milliseconds'")]
            Milliseconds,
            [SqlName("'second'")]
            Second,
            [SqlName("'minute'")]
            Minute,
            [SqlName("'hour'")]
            Hour,
            [SqlName("'day'")]
            Day,
            [SqlName("'week'")]
            Week,
            [SqlName("'month'")]
            Month,
            [SqlName("'quarter'")]
            Quarter,
            [SqlName("'year'")]
            Year,
            [SqlName("'decade'")]
            Decade,
            [SqlName("'century'")]
            Century,
            [SqlName("'millennium'")]
            Millennium
        }
        [SqlName("date_trunc")]
        public static T DateTrunc<T>(DateTruncField field, T value) => throw new SqlFunctionException();
    }
}
