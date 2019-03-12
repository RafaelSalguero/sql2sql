using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using LinqKit;
using SqlToSql.Fluent.Data;

namespace SqlToSql.Fluent
{
    public static class Sql
    {
        public static ISqlJoinAble<T1> From<T1>(IFromListItemTarget<T1> from) =>
            new PreSelectPreWinBuilder<T1>(new PreSelectClause<T1, object>(new SqlFrom<T1>(from), SelectType.All, null, null));

        public static T Over<T>(T expr, ISqlWindow over) => throw new SqlFunctionException();

        [SqlFunction("count")]
        public static int Count<T>(T expr) => throw new SqlFunctionException();
        [SqlFunction("sum")]
        public static T Sum<T>(T expr) => throw new SqlFunctionException();
        [SqlFunction("max")]
        public static T Max<T>(T expr) => throw new SqlFunctionException();
        [SqlFunction("min")]
        public static T Min<T>(T expr) => throw new SqlFunctionException();
    }
}
