using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using KeaSql.Fluent;
using KeaSql.Fluent.Data;
using KeaSql.SqlText;

namespace KeaSql
{
    /// <summary>
    /// Extensiones de SQL
    /// </summary>
    public static class SqlExtensions
    {
        /// <summary>
        /// Indica que un subquery es escalar, por lo que se puede usar dentro de expresiones de Select
        /// </summary>
        public static T Scalar<T>(this ISqlSubQuery<T> subquery) =>
            throw new SqlFunctionException();

        /// <summary>
        /// Obtiene el SQL y los parámetros de un select
        /// </summary>
        public static SqlResult ToSql(this ISqlSelect select)
        {
            var mode = ParamMode.EntityFramework;
            var dic = new SqlParamDic();
            var sql = SqlText.SqlSelect.SelectToString(select.Clause, mode, dic);
            var pars = dic.Items.Select(x => new SqlParam(x.ParamName, x.GetValue()));

            return new SqlResult(sql, pars.ToList());
        }

        //Joins:
        /// <summary>
        /// Inicia un INNER JOIN
        /// </summary>
        public static IJoinLateralAble<T1> Inner<T1>(this ISqlJoinAble<T1> left) =>
            new JoinItems<T1, object>(JoinType.Inner, false, left, null);

        /// <summary>
        /// Inicia un LEFT JOIN
        /// </summary>
        public static IJoinLateralAble<T1> Left<T1>(this ISqlJoinAble<T1> left) =>
            new JoinItems<T1, object>(JoinType.Left, false, left, null);

        /// <summary>
        /// Inicia un RIGHT JOIN
        /// </summary>
        public static IJoinLateralAble<T1> Right<T1>(this ISqlJoinAble<T1> left) =>
            new JoinItems<T1, object>(JoinType.Left, false, left, null);

        /// <summary>
        /// Inicia un CROSS JOIN
        /// </summary>
        public static IJoinLateralAble<T1> Cross<T1>(this ISqlJoinAble<T1> left) =>
            new JoinItems<T1, object>(JoinType.Cross, false, left, null);

        /// <summary>
        /// Inicia un OUTTER JOIN
        /// </summary>
        public static IJoinLateralAble<T1> Outter<T1>(this ISqlJoinAble<T1> left) =>
            new JoinItems<T1, object>(JoinType.Cross, false, left, null);

        public static IJoinOnAble<TL, TR> Join<TL, TR>(this IJoinLateralAble<TL> left, IFromListItemTarget<TR> right)
        {
            var dummyP0 = Expression.Parameter(typeof(TL));
            var r = Expression.Lambda<Func<TL, IFromListItemTarget<TR>>>(Expression.Constant(right), dummyP0);

            return new JoinItems<TL, TR>(left.Type, false, left.Left, r);
        }

        public static IJoinOnAble<TL, TR> Lateral<TL, TR>(this IJoinLateralAble<TL> left, Expression<Func<TL, IFromListItemTarget<TR>>> right) =>
          new JoinItems<TL, TR>(left.Type, true, left.Left, right);



        #region Joins Ons
        public static ISqlJoinAble<TRet> OnMap<T1, T2, TRet>(this IJoinOnAble<T1, T2> items, Expression<Func<T1, T2, TRet>> map, Expression<Func<TRet, bool>> on)
        {
            var it = new SqlJoin<T1, T2, TRet>(items.Left.Clause.From, items.Right, map, on, items.Type, items.Lateral);
            return new PreSelectPreWinBuilder<TRet>(new PreSelectClause<TRet, object>(it, SelectType.All, null, null));
        }

        public static ISqlJoinAble<Tuple<T1, T2>> OnTuple<T1, T2>(this IJoinOnAble<T1, T2> items, Expression<Func<Tuple<T1, T2>, bool>> on) =>
             items.OnMap((a, b) => new Tuple<T1, T2>(a, b), on);

        public static ISqlJoinAble<Tuple<T1, T2>> On<T1, T2>(this IJoinOnAble<Tuple<T1>, T2> items, Expression<Func<Tuple<T1, T2>, bool>> on) =>
            items.OnMap((a, b) => new Tuple<T1, T2>(a.Item1, b), on);


        public static ISqlJoinAble<Tuple<T1, T2, T3>> On<T1, T2, T3>(this IJoinOnAble<Tuple<T1, T2>, T3> items, Expression<Func<Tuple<T1, T2, T3>, bool>> on) =>
            items.OnMap((a, b) => new Tuple<T1, T2, T3>(a.Item1, a.Item2, b), on);

        public static ISqlJoinAble<Tuple<T1, T2, T3, T4>> On<T1, T2, T3, T4>(this IJoinOnAble<Tuple<T1, T2, T3>, T4> items, Expression<Func<Tuple<T1, T2, T3, T4>, bool>> on) =>
            items.OnMap((a, b) => new Tuple<T1, T2, T3, T4>(a.Item1, a.Item2, a.Item3, b), on);

        public static ISqlJoinAble<Tuple<T1, T2, T3, T4, T5>> On<T1, T2, T3, T4, T5>(this IJoinOnAble<Tuple<T1, T2, T3, T4>, T5> items, Expression<Func<Tuple<T1, T2, T3, T4, T5>, bool>> on) =>
            items.OnMap((a, b) => new Tuple<T1, T2, T3, T4, T5>(a.Item1, a.Item2, a.Item3, a.Item4, b), on);

        public static ISqlJoinAble<Tuple<T1, T2, T3, T4, T5, T6>> On<T1, T2, T3, T4, T5, T6>(this IJoinOnAble<Tuple<T1, T2, T3, T4, T5>, T6> items, Expression<Func<Tuple<T1, T2, T3, T4, T5, T6>, bool>> on) =>
            items.OnMap((a, b) => new Tuple<T1, T2, T3, T4, T5, T6>(a.Item1, a.Item2, a.Item3, a.Item4, a.Item5, b), on);

        public static ISqlJoinAble<TOut> Alias<TIn, TOut>(this ISqlJoinAble<TIn> from, Expression<Func<TIn, TOut>> map)
        {
            var it = new FromListAlias<TIn, TOut>(from.Clause.From, map);
            return new PreSelectPreWinBuilder<TOut>(new PreSelectClause<TOut, object>(it, SelectType.All, null, null));
        }
        #endregion



        #region Select
        public static ISqlFirstWindowAble<T> Distinct<T>(this ISqlDistinctAble<T> input) => new PreSelectPreWinBuilder<T>(input.Clause.SetType(SelectType.Distinct));
        public static ISqlDistinctOnThenByAble<T> DistinctOn<T>(this ISqlDistinctAble<T> input, Expression<Func<T, object>> expr) => new PreSelectPreWinBuilder<T>(input.Clause.AddDistinctOn(expr));


        public static ISqlWherable<TIn, TOut, TWin> Select<TIn, TOut, TWin>(this ISqlSelectAble<TIn, TWin> input, Expression<Func<TIn, TOut>> select) =>
                new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.SetSelect(select));

        public static ISqlWherable<TIn, TOut, TWin> Select<TIn, TOut, TWin>(this ISqlSelectAble<TIn, TWin> input, Expression<Func<TIn, TWin, TOut>> select) =>
                new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.SetSelect(select));


        public static ISqlGroupByThenByAble<TIn, TOut, TWin> GroupBy<TIn, TOut, TWin>(this ISqlGroupByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr) =>
            new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.AddGroupBy(new GroupByExpr<TIn>(expr)));

        public static ISqlGroupByThenByAble<TIn, TOut, TWin> ThenBy<TIn, TOut, TWin>(this ISqlGroupByThenByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr) =>
            new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.AddGroupBy(new GroupByExpr<TIn>(expr)));

        public static ISqlOrderByThenByAble<TIn, TOut, TWin> OrderBy<TIn, TOut, TWin>(this ISqlOrderByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr, OrderByOrder order, OrderByNulls? nulls) =>
            new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.AddOrderBy(new OrderByExpr<TIn>(expr, order, nulls)));

        public static ISqlOrderByThenByAble<TIn, TOut, TWin> OrderBy<TIn, TOut, TWin>(this ISqlOrderByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr, OrderByOrder order) =>
            input.OrderBy(expr, order, null);

        public static ISqlOrderByThenByAble<TIn, TOut, TWin> OrderBy<TIn, TOut, TWin>(this ISqlOrderByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr) =>
            input.OrderBy(expr, OrderByOrder.Asc);

        public static ISqlOrderByThenByAble<TIn, TOut, TWin> ThenBy<TIn, TOut, TWin>(this ISqlOrderByThenByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr, OrderByOrder order, OrderByNulls? nulls) =>
            new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.AddOrderBy(new OrderByExpr<TIn>(expr, order, nulls)));

        public static ISqlOrderByThenByAble<TIn, TOut, TWin> ThenBy<TIn, TOut, TWin>(this ISqlOrderByThenByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr, OrderByOrder order) =>
            input.ThenBy(expr, order, null);

        public static ISqlOrderByThenByAble<TIn, TOut, TWin> ThenBy<TIn, TOut, TWin>(this ISqlOrderByThenByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr) =>
            input.ThenBy(expr, OrderByOrder.Asc);

        public static ISqlGroupByAble<TIn, TOut, TWin> Where<TIn, TOut, TWin>(this ISqlWherable<TIn, TOut, TWin> input, Expression<Func<TIn, bool>> where) =>
                new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.SetWhere(where));

        public static ISqlGroupByAble<TIn, TOut, TWin> Where<TIn, TOut, TWin>(this ISqlWherable<TIn, TOut, TWin> input, Expression<Func<TIn, TWin, bool>> where) =>
                new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.SetWhere(where));

        public static ISqlSelect<TIn, TOut, TWin> Limit<TIn, TOut, TWin>(this ISqlLimitAble<TIn, TOut, TWin> input, int limit) =>
                new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.SetLimit(limit));
        #endregion

        #region Window
        public static ISqlSelectAble<TIn, TWinOut> Window<TIn, TWinIn, TWinOut>(this ISqlWindowAble<TIn, TWinIn> input, Func<ISqlWindowExistingAble<TIn, TWinIn>, TWinOut> windows)
        {
            var builder = new SqlWindowBuilder<TIn, TWinIn>(input.Clause.Window, new SqlWindowClause<TIn, TWinIn>(null, new PartitionByExpr<TIn>[0], new OrderByExpr<TIn>[0], null));
            var ws = new WindowClauses<TWinOut>(windows(builder));
            return new SqlPreSelectBuilder<TIn, TWinOut>(input.Clause.SetWindow(ws));
        }

        public static ISqlWindowPartitionByThenByAble<TIn, TWin> PartitionBy<TIn, TWin>(this ISqlWindowPartitionByAble<TIn, TWin> input, Expression<Func<TIn, object>> expr)
        {
            var old = new List<PartitionByExpr<TIn>>();
            old.Add(new PartitionByExpr<TIn>(expr));
            return new SqlWindowBuilder<TIn, TWin>(input.Input, input.Current.SetPartitionBy(old));
        }
        public static ISqlWindowPartitionByThenByAble<TIn, TWin> ThenBy<TIn, TWin>(this ISqlWindowPartitionByThenByAble<TIn, TWin> input, Expression<Func<TIn, object>> expr)
        {
            var old = input.Current.PartitionBy.ToList();
            old.Add(new PartitionByExpr<TIn>(expr));
            return new SqlWindowBuilder<TIn, TWin>(input.Input, input.Current.SetPartitionBy(old));
        }
        public static ISqlWindowOrderByThenByAble<TIn, TWin> OrderBy<TIn, TWin>(this ISqlWindowOrderByAble<TIn, TWin> input, Expression<Func<TIn, object>> expr, OrderByOrder order = OrderByOrder.Asc, OrderByNulls? nulls = null)
        {
            var old = new List<OrderByExpr<TIn>>();
            old.Add(new OrderByExpr<TIn>(expr, order, nulls));
            return new SqlWindowBuilder<TIn, TWin>(input.Input, input.Current.SetOrderBy(old));
        }
        public static ISqlWindowOrderByThenByAble<TIn, TWin> ThenBy<TIn, TOut, TWin>(this ISqlWindowOrderByThenByAble<TIn, TWin> input, Expression<Func<TIn, object>> expr, OrderByOrder order = OrderByOrder.Asc, OrderByNulls? nulls = null)
        {
            var old = input.Current.OrderBy.ToList();
            old.Add(new OrderByExpr<TIn>(expr, order, nulls));
            return new SqlWindowBuilder<TIn, TWin>(input.Input, input.Current.SetOrderBy(old));
        }
        static ISqlWindowFrameStartBetweenAble<TIn, TWin> FrameGrouping<TIn, TWin>(this ISqlWindowFrameAble<TIn, TWin> input, WinFrameGrouping grouping)
        {
            var newFrame = new SqlWinFrame(grouping, null, null, null);
            return new SqlWindowBuilder<TIn, TWin>(input.Input, input.Current.SetFrame(newFrame));
        }

        public static ISqlWindowFrameStartBetweenAble<TIn, TWin> Range<TIn, TOut, TWin>(this ISqlWindowFrameAble<TIn, TWin> input) =>
                input.FrameGrouping(WinFrameGrouping.Range);

        public static ISqlWindowFrameStartBetweenAble<TIn, TWin> Rows<TIn, TWin>(this ISqlWindowFrameAble<TIn, TWin> input) =>
                input.FrameGrouping(WinFrameGrouping.Rows);

        public static ISqlWindowFrameStartBetweenAble<TIn, TWin> Groups<TIn, TOut, TWin>(this ISqlWindowFrameAble<TIn, TWin> input) =>
                   input.FrameGrouping(WinFrameGrouping.Groups);

        static ISqlWindowFrameEndExclusionAble<TIn, TWin> Start<TIn, TWin>(this ISqlWindowFrameStartAble<TIn, TWin> input, WinFrameStartEnd startEnd, int? offset = null)
        {
            var old = input.Current.Frame;
            return new SqlWindowBuilder<TIn, TWin>(input.Input, input.Current.SetFrame(old.SetStart(new SqlWindowFrameStartEnd(startEnd, offset))));
        }

        static ISqlWindowFrameExclusionAble<TIn, TWin> End<TIn, TWin>(this ISqlWindowFrameEndAble<TIn, TWin> input, WinFrameStartEnd startEnd, int? offset = null)
        {
            var old = input.Current.Frame;
            return new SqlWindowBuilder<TIn, TWin>(input.Input, input.Current.SetFrame(old.SetEnd(new SqlWindowFrameStartEnd(startEnd, offset))));
        }

        //START:

        public static ISqlWindowFrameEndExclusionAble<TIn, TWin> UnboundedPreceding<TIn, TWin>(this ISqlWindowFrameStartAble<TIn, TWin> input) =>
                input.Start(WinFrameStartEnd.UnboundedPreceding);

        public static ISqlWindowFrameEndExclusionAble<TIn, TWin> Preceding<TIn, TWin>(this ISqlWindowFrameStartAble<TIn, TWin> input, int offset) =>
                input.Start(WinFrameStartEnd.OffsetPreceding, offset);

        public static ISqlWindowFrameEndExclusionAble<TIn, TWin> CurrentRow<TIn, TWin>(this ISqlWindowFrameStartAble<TIn, TWin> input) =>
                input.Start(WinFrameStartEnd.CurrentRow);

        public static ISqlWindowFrameEndExclusionAble<TIn, TWin> Following<TIn, TWin>(this ISqlWindowFrameStartAble<TIn, TWin> input, int offset) =>
                input.Start(WinFrameStartEnd.OffsetFollowing, offset);

        public static ISqlWindowFrameEndExclusionAble<TIn, TWin> UnboundedFollowing<TIn, TWin>(this ISqlWindowFrameStartAble<TIn, TWin> input) =>
                input.Start(WinFrameStartEnd.UnboundedFollowing);


        //END:
        public static ISqlWindowFrameExclusionAble<TIn, TWin> AndUnboundedPreceding<TIn, TWin>(this ISqlWindowFrameEndAble<TIn, TWin> input) =>
             input.End(WinFrameStartEnd.UnboundedFollowing);

        public static ISqlWindowFrameExclusionAble<TIn, TWin> AndPreceding<TIn, TWin>(this ISqlWindowFrameEndAble<TIn, TWin> input, int offset) =>
            input.End(WinFrameStartEnd.OffsetPreceding, offset);

        public static ISqlWindowFrameExclusionAble<TIn, TWin> AndCurrentRow<TIn, TWin>(this ISqlWindowFrameEndAble<TIn, TWin> input) =>
            input.End(WinFrameStartEnd.CurrentRow);

        public static ISqlWindowFrameExclusionAble<TIn, TWin> AndFollowing<TIn, TWin>(this ISqlWindowFrameEndAble<TIn, TWin> input, int offset) =>
            input.End(WinFrameStartEnd.OffsetFollowing, offset);

        public static ISqlWindowFrameExclusionAble<TIn, TWin> AndUnboundedFollowing<TIn, TWin>(this ISqlWindowFrameEndAble<TIn, TWin> input) =>
            input.End(WinFrameStartEnd.UnboundedFollowing);

        //Exclusion:

        static ISqlWindowFrame<TIn, TWin> Exclusion<TIn, TWin>(this ISqlWindowFrameExclusionAble<TIn, TWin> input, WinFrameExclusion exclusion)
        {
            var old = input.Current.Frame;
            return new SqlWindowBuilder<TIn, TWin>(input.Input, input.Current.SetFrame(old.SetExclusion(exclusion)));
        }

        public static ISqlWindowFrame<TIn, TWin> ExcludeCurrentRow<TIn, TWin>(this ISqlWindowFrameExclusionAble<TIn, TWin> input) =>
            input.Exclusion(WinFrameExclusion.CurrentRow);

        public static ISqlWindowFrame<TIn, TWin> ExcludeGroup<TIn, TWin>(this ISqlWindowFrameExclusionAble<TIn, TWin> input) =>
            input.Exclusion(WinFrameExclusion.Group);

        public static ISqlWindowFrame<TIn, TWin> ExcludeTies<TIn, TWin>(this ISqlWindowFrameExclusionAble<TIn, TWin> input) =>
            input.Exclusion(WinFrameExclusion.Ties);

        public static ISqlWindowFrame<TIn, TWin> ExcludeNoOthers<TIn, TWin>(this ISqlWindowFrameExclusionAble<TIn, TWin> input) =>
            input.Exclusion(WinFrameExclusion.NoOthers);
        #endregion
    }
}
