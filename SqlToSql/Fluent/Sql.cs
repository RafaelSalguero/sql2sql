using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using LinqKit;
using SqlToSql.Fluent.Data;
using SqlToSql.PgLan;

namespace SqlToSql.Fluent
{
    public enum DateTrunc
    {

    }
    public static class Sql
    {
        public static ISqlJoinAble<TTable> From<TTable>() => From(new SqlTable<TTable>());
        public static ISqlJoinAble<T1> From<T1>(IFromListItemTarget<T1> from) =>
            new PreSelectPreWinBuilder<T1>(new PreSelectClause<T1, object>(new SqlFrom<T1>(from), SelectType.All, null, null));

        /// <summary>
        /// Sustituir la cadena especificada
        /// </summary>
        public static object Raw(string sql) => throw new SqlFunctionException();
        public static T Raw<T>(string sql) => throw new SqlFunctionException();
        public static T Over<T>(T expr, ISqlWindow over) => throw new SqlFunctionException();
        public static T Cast<T>(T expr, SqlType type) => throw new SqlFunctionException();

        [SqlNameAttribute("count")]
        public static int Count<T>(T expr) => throw new SqlFunctionException();
        [SqlNameAttribute("sum")]
        public static T Sum<T>(T expr) => throw new SqlFunctionException();
        [SqlNameAttribute("max")]
        public static T Max<T>(T expr) => throw new SqlFunctionException();
        [SqlNameAttribute("min")]
        public static T Min<T>(T expr) => throw new SqlFunctionException();

        [SqlNameAttribute("round")]
        public static T Round<T>(T value, int places) => throw new SqlFunctionException();

        [SqlNameAttribute("coalesce")]
        public static T Coalesce<T>(T? a, T b) where T : struct => throw new SqlFunctionException();
        [SqlNameAttribute("coalesce")]
        public static T Coalesce<T>(T? a, T? b, T c) where T : struct => throw new SqlFunctionException();
        [SqlNameAttribute("coalesce")]
        public static T Coalesce<T>(T a, T b) => throw new SqlFunctionException();
        [SqlNameAttribute("coalesce")]
        public static T Coalesce<T>(T a, T b, T c) => throw new SqlFunctionException();

        [SqlNameAttribute("greatest")]
        public static T Greatest<T>(T a, T b) => throw new SqlFunctionException();
        [SqlNameAttribute("greatest")]
        public static T Greatest<T>(T a, T b, T c) => throw new SqlFunctionException();
        [SqlNameAttribute("greatest")]
        public static T Greatest<T>(T a, T b, T c, T d) => throw new SqlFunctionException();

        [SqlNameAttribute("least")]
        public static T Least<T>(T a, T b) => throw new SqlFunctionException();
        [SqlNameAttribute("least")]
        public static T Least<T>(T a, T b, T c) => throw new SqlFunctionException();
        [SqlNameAttribute("least")]
        public static T Least<T>(T a, T b, T c, T d) => throw new SqlFunctionException();

        public enum DateTruncField
        {
            [SqlNameAttribute("microseconds")]
            Microseconds,
            [SqlNameAttribute("milliseconds")]
            Milliseconds,
            [SqlNameAttribute("second")]
            Second,
            [SqlNameAttribute("minute")]
            Minute,
            [SqlNameAttribute("hour")]
            Hour,
            [SqlNameAttribute("day")]
            Day,
            [SqlNameAttribute("week")]
            Week,
            [SqlNameAttribute("month")]
            Month,
            [SqlNameAttribute("quarter")]
            Quarter,
            [SqlNameAttribute("year")]
            Year,
            [SqlNameAttribute("decade")]
            Decade,
            [SqlNameAttribute("century")]
            Century,
            [SqlNameAttribute("millennium")]
            Millennium
        }
        [SqlNameAttribute("date_trunc")]
        public static T DateTrunc<T>(DateTruncField field, T value) => throw new SqlFunctionException();
    }
}
