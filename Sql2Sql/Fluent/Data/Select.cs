using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sql2Sql.ExprTree;

namespace Sql2Sql.Fluent.Data
{
    public interface ISqlSelectBuilder<TIn, TOut, TWin> :
         ISqlSelectHasClause<TIn, TOut, TWin>, ISqlOrderByThenByAble<TIn, TOut, TWin>, ISqlOrderByAble<TIn, TOut, TWin>, ISqlGroupByAble<TIn, TOut, TWin>,
         ISqlWherable<TIn, TOut, TWin>, ISqlGroupByThenByAble<TIn, TOut, TWin>,
         ISqlSelectAble<TIn, TOut, TWin>, ISqlWindowAble<TIn, TOut, TWin>,
         ISqlNextJoinAble<TIn, TOut, TWin>, ISqlFirstJoinAble<TIn, TOut, TWin>, ISqlDistinctOnThenByAble<TIn, TOut, TWin>
    {
    }


    class JoinNotSupportedException : ArgumentException
    {
        public JoinNotSupportedException() : base("JOIN not supported at this stage of the query builder") { }
    }

    public class SqlSelectBuilder<TIn, TOut, TWin> : ISqlSelectBuilder<TIn, TOut, TWin>
    {
        public SqlSelectBuilder(SelectClause clause)
        {

            Clause = clause;
        }

        public SelectClause Clause { get; }

        JoinItems<TOut, TR1> InternalJoin<TR1>(string table)
        {

            if (this is ISqlSelectBuilder<TOut, TOut, object> x)
            {
                return x.InternalInner().InternalJoin(new SqlTable<TR1>(table));
            }
            throw new JoinNotSupportedException();
        }

        IFirstJoinOnAble<TOut, TR1> IFirstJoinAble<TOut>.Join<TR1>() => InternalJoin<TR1>(null);
        IFirstJoinOnAble<TOut, TR1> IFirstJoinAble<TOut>.Join<TR1>(string table) => InternalJoin<TR1>(table);

        INextJoinOnAble<TOut, TR1> INextJoinAble<TOut>.Join<TR1>() => InternalJoin<TR1>(null);
        INextJoinOnAble<TOut, TR1> INextJoinAble<TOut>.Join<TR1>(string table) => InternalJoin<TR1>(table);

        public override string ToString()
        {
            return this.ToSql(SqlText.ParamMode.Substitute).Sql;
        }
    }

    public enum SelectType
    {
        All,
        Distinct,
        DistinctOn
    }


    public class WithSelectClause
    {
        public WithSelectClause(ParameterExpression param, ISqlWith with)
        {
            Param = param;
            With = with;
        }

        public ParameterExpression Param { get; }
        public ISqlWith With { get; }
    }

    public class SelectClause
    {
        public SelectClause(IFromListItem from, SelectType distinctType, IReadOnlyList<LambdaExpression> distinctOn, WindowClauses window, LambdaExpression select, LambdaExpression where, IReadOnlyList<GroupByExpr> groupBy, IReadOnlyList<OrderByExpr> orderBy, int? limit)
        {
            if (select == null)
                throw new ArgumentNullException(nameof(select));

            From = from;
            DistinctType = distinctType;
            DistinctOn = distinctOn;
            Window = window;
            Select = select;
            Where = where;
            GroupBy = groupBy;
            OrderBy = orderBy;
            Limit = limit;
        }

        public IFromListItem From { get; }
        public SelectType DistinctType { get; }

        /// <summary>
        /// Each item is a (in) => ret expression where in is the SELECT parameter
        /// and ret is the DISTINCT ON expression
        /// </summary>
        public IReadOnlyList<LambdaExpression> DistinctOn { get; }
        public WindowClauses Window { get; }

        /// <summary>
        /// SELECT expression
        /// (in, win) => ret 
        /// 'in' is the SELECT parameter
        /// 'win' is the named WINDOW object
        /// 'ret' is the SELECT expression
        /// 
        /// Can be null, if null, represents an star (*) expression
        /// </summary>
        public LambdaExpression Select { get; }

        /// <summary>
        /// WHERE expression
        /// (in, win) => ret
        /// 'in' is the SELECT parameter
        /// 'win' is the named WINDOW objecty
        /// 'ret' is a boolean expression representing the WHERE expression
        /// </summary>
        public LambdaExpression Where { get; }


        public IReadOnlyList<GroupByExpr> GroupBy { get; }
        public IReadOnlyList<OrderByExpr> OrderBy { get; }
        public int? Limit { get; }

        public SelectClause SetFrom(IFromListItem fromItem) => Immutable.Set(this, x => x.From, fromItem);

        public SelectClause SetSelect<TIn, TWin, TOut>(Expression<Func<TIn, TWin, TOut>> select) => Immutable.Set(this, x => x.Select, select);
        public SelectClause SetSelect<TIn, TOut>(Expression<Func<TIn, TOut>> select) => SetSelect(ExprHelper.AddParam<TIn, object, TOut>(select));

        public SelectClause SetWhere<TIn, TWin>(Expression<Func<TIn, TWin, bool>> value) => Immutable.Set(this, x => x.Where, value);
        public SelectClause SetWhere<TIn>(Expression<Func<TIn, bool>> expr) => SetWhere(ExprHelper.AddParam<TIn, object, bool>(expr));

        public SelectClause SetWindow(WindowClauses window) => Immutable.Set(this, x => x.Window, window);
        public SelectClause SetDistinctType(SelectType type) => Immutable.Set(this, x => x.DistinctType, type);
        public SelectClause AddDistinctOn(LambdaExpression distinctOn) => Immutable.Add(this.SetDistinctType(SelectType.DistinctOn), x => x.DistinctOn, distinctOn);

        public SelectClause AddOrderBy(OrderByExpr value) => Immutable.Add(this, x => x.OrderBy, value);
        public SelectClause AddGroupBy(GroupByExpr value) => Immutable.Add(this, x => x.GroupBy, value);

        public SelectClause SetLimit(int? value) => Immutable.Set(this, x => x.Limit, value);

        /// <summary>
        /// Returns the default SELECT expr
        /// </summary>
        public static Expression<Func<TIn, TWin, TIn>> DefaultSelectExpr<TIn, TWin>() => (x, win) => x;

        /// <summary>
        /// Returns the default SELECT expr
        /// </summary>
        public static Expression<Func<TIn, object, TIn>> DefaultSelectExpr<TIn>() => DefaultSelectExpr<TIn, object>();

    }

}
