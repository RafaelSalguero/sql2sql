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
    public static class Sql2
    {
        public static ISqlJoinAble<T1> From<T1>(IFromListItemTarget<T1> from) =>
            new PreSelectPreWinBuilder<T1>(new PreSelectClause<T1, object>(new SqlFrom<T1>(from), SelectType.All, null, null));

        //Joins:
        public static JoinItems<T1, T2> Join<T1, T2>(this ISqlJoinAble<T1> left, IFromListItemTarget<T2> right) =>
            new JoinItems<T1, T2>(JoinType.Inner, left, right);

        #region Joins Ons
        public static ISqlJoinAble<TRet> On<T1, T2, TRet>(this JoinItems<T1, T2> items, Expression<Func<T1, T2, TRet>> map, Expression<Func<TRet, bool>> on)
        {
            var it = new SqlJoin<T1, T2, TRet>(items.Left.Clause.From, items.Right, map, on);
            return new PreSelectPreWinBuilder<TRet>(new PreSelectClause<TRet, object>(it, SelectType.All, null, null));
        }


        public static ISqlJoinAble<Tuple<T1, T2>> On<T1, T2>(this JoinItems<T1, T2> items, Expression<Func<Tuple<T1, T2>, bool>> on) =>
             items.On((a, b) => new Tuple<T1, T2>(a, b), on);

        public static ISqlJoinAble<Tuple<T1, T2>> On<T1, T2>(this JoinItems<Tuple<T1>, T2> items, Expression<Func<Tuple<T1, T2>, bool>> on) =>
            items.On((a, b) => new Tuple<T1, T2>(a.Item1, b), on);
        public static ISqlJoinAble<Tuple<T1, T2, T3>> On<T1, T2, T3>(this JoinItems<Tuple<T1, T2>, T3> items, Expression<Func<Tuple<T1, T2, T3>, bool>> on) =>
            items.On((a, b) => new Tuple<T1, T2, T3>(a.Item1, a.Item2, b), on);
        public static ISqlJoinAble<Tuple<T1, T2, T3, T4>> On<T1, T2, T3, T4>(this JoinItems<Tuple<T1, T2, T3>, T4> items, Expression<Func<Tuple<T1, T2, T3, T4>, bool>> on) =>
            items.On((a, b) => new Tuple<T1, T2, T3, T4>(a.Item1, a.Item2, a.Item3, b), on);

        public static ISqlJoinAble<TOut> Alias<TIn, TOut>(this ISqlJoinAble<TIn> from, Expression<Func<TIn, TOut>> map)
        {
            var it = new FromListAlias<TIn, TOut>(from.Clause.From, map);
            return new PreSelectPreWinBuilder<TOut>(new PreSelectClause<TOut, object>(it, SelectType.All, null, null));
        }
        #endregion



        #region Select


        public static ISqlWherable<TIn, TOut, TWin> Select<TIn, TOut, TWin>(this ISqlSelectAble<TIn, TWin> input, Expression<Func<TIn, TOut>> select) =>
                new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.SetSelect(select));

        public static ISqlGroupByAble<TIn, TOut, TWin> Where<TIn, TOut, TWin>(this ISqlWherable<TIn, TOut, TWin> input, Expression<Func<TIn, bool>> where) =>
                new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.SetWhere(where));

        #endregion

        #region Window
        public static ISqlSelectAble<TIn, TWinOut> Window<TIn, TWinIn, TWinOut>(this ISqlWindowAble<TIn, TWinIn> input, Func<ISqlWindowExistingAble<TIn, TWinIn>, TWinOut> windows)
        {
            var builder = new SqlWindowBuilder<TIn, TWinIn>(input.Clause.Window, new SqlWindowClause<TIn, TWinIn>(null, null, null, null));
            var ws = new WindowClauses<TWinOut>(windows(builder));
            return new SqlPreSelectBuilder<TIn, TWinOut>(input.Clause.SetWindow(ws));
        }

        public static ISqlWindowPartitionByThenByAble<TIn, TWin> PartitionBy<TIn, TOut, TWin>(this ISqlWindowPartitionByAble<TIn, TWin> input, Expression<Func<TIn, object>> expr)
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
            var old = input.Current.Frame;
            var newFrame = new SqlWinFrame(grouping, old.Start, old.End, old.Exclusion);
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
                input.Start(WinFrameStartEnd.UnboundedFollowing);

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
