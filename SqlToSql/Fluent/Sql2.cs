using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using LinqKit;

namespace SqlToSql.Fluent
{
    public static class Sql2
    {
        public static FromListFrom<T1> From<T1>(IFromListItemTarget<T1> from) =>
            new FromListFrom<T1> ( new SqlFrom<T1>( from));

        //Joins:
        public static JoinItems<T1, T2> Join<T1, T2>(this IFromListJoinAble<T1> left, IFromListItemTarget<T2> right) =>
           new JoinItems<T1, T2>(JoinType.Inner, left, right);

        #region Joins Ons
        public static FromListJoin<TRet> On<T1, T2, TRet>(this JoinItems<T1, T2> items, Expression<Func<T1, T2, TRet>> map, Expression<Func<TRet, bool>> on)
        {
            var it = new SqlJoin<T1, T2, TRet>(items.Left.Clause.From, items.Right, map, on);
            return new FromListJoin<TRet> (it);
        }


        public static FromListJoin<Tuple<T1, T2>> On<T1, T2>(this JoinItems<T1, T2> items, Expression<Func<Tuple<T1, T2>, bool>> on) =>
             items.On((a, b) => new Tuple<T1, T2>(a, b), on);

        public static FromListJoin<Tuple<T1, T2>> On<T1, T2>(this JoinItems<Tuple<T1>, T2> items, Expression<Func<Tuple<T1, T2>, bool>> on) =>
            items.On((a, b) => new Tuple<T1, T2>(a.Item1,  b), on);
        public static FromListJoin<Tuple<T1, T2, T3>> On<T1, T2, T3>(this JoinItems<Tuple<T1, T2>, T3> items, Expression<Func<Tuple<T1, T2, T3>, bool>> on) =>
            items.On((a, b) => new Tuple<T1, T2, T3>(a.Item1, a.Item2, b), on);
        public static FromListJoin<Tuple<T1, T2, T3, T4>> On<T1, T2, T3, T4>(this JoinItems<Tuple<T1, T2, T3>, T4> items, Expression<Func<Tuple<T1, T2, T3, T4>, bool>> on) =>
            items.On((a, b) => new Tuple<T1, T2, T3, T4>(a.Item1, a.Item2, a.Item3, b), on);

        public static FromListAlias<TOut> Alias<TIn, TOut>(this FromListJoin<TIn> from, Expression<Func<TIn, TOut>> map)
        {
            return new FromListAlias<TOut>(new FromListAlias<TIn, TOut>(from.Clause.From, map));
        }
        #endregion



        #region Select
        public static SqlDistinctOn<T> DistinctOn<T>(this ISqlDistinctOnAble<T> input, Expression<Func<T, object>> distinctOn) =>
                new SqlDistinctOn<T>(input.Clause, distinctOn);

        public static SqlDistinct<T> Distinct<T>(this ISqlDistinctAble<T> input) =>
                new SqlDistinct<T>(input.Clause);

        public static SqlSelect<TIn, TOut, object> Select<TIn, TOut>(this ISqlSelectAble<TIn> input, Expression<Func<TIn, TOut>> select) =>
                new SqlSelect<TIn, TOut,object>(input.Clause, select);

        public static SqlWhere<TIn, TOut, TWin> Where<TIn, TOut, TWin>(this ISqlWherable<TIn, TOut, TWin> input, Expression<Func<TIn, bool>> where) =>
                new SqlWhere<TIn, TOut, TWin>(input.Clause, where);

        public static SqlGroupBy<TIn, TOut, TWin> GroupBy<TIn, TOut, TWin>(this ISqlGroupByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> group) =>
                new SqlGroupBy<TIn, TOut, TWin>(input.Clause, group);

        public static SqlOrderBy<TIn, TOut, TWin> OrderBy<TIn, TOut, TWin>(this ISqlOrderByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr, OrderByOrder order = OrderByOrder.Asc, OrderByNulls? nulls = null) =>
                 new SqlOrderBy<TIn, TOut, TWin>(input.Clause, new OrderByExpr<TIn>(expr, order, nulls));

        public static SqlOrderBy<TIn, TOut, TWin> ThenBy<TIn, TOut, TWin>(this ISqlOrderByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr, OrderByOrder order = OrderByOrder.Asc, OrderByNulls? nulls = null) =>
                 new SqlOrderBy<TIn, TOut, TWin>(input.Clause, new OrderByExpr<TIn>(expr, order, nulls));

        public static SqlLimit<TIn, TOut, TWin> Limit<TIn, TOut, TWin>(this ISqlLimitAble<TIn, TOut, TWin> input, int limit) =>
                new SqlLimit<TIn, TOut, TWin>(input.Clause, limit);
        #endregion

        #region Window
        #endregion
    }

}
