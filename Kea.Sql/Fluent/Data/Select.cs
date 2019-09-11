using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using KeaSql.ExprTree;

namespace KeaSql.Fluent.Data
{
    public interface ISqlSelectBuilder<TIn, TOut, TWin> :
         ISqlSelectHasClause<TIn, TOut, TWin>, ISqlOrderByThenByAble<TIn, TOut, TWin>, ISqlOrderByAble<TIn, TOut, TWin>, ISqlGroupByAble<TIn, TOut, TWin>,
         ISqlWherable<TIn, TOut, TWin>, ISqlGroupByThenByAble<TIn, TOut, TWin>, 
        ISqlSelectAble<TIn, TOut, TWin>, ISqlWindowAble<TIn, TOut, TWin>,
         ISqlJoinAble<TIn, TOut, TWin>,  ISqlDistinctOnThenByAble<TIn, TOut, TWin>
    {

    /*,
     ISqlSelectAble<TIn, TWin>, ISqlWindowAble<TIn, TWin>,
     ISqlJoinAble<TIn>, ISqlDistinctDistinctOnAble<TIn>, ISqlDistinctOnThenByAble<TIn>*/

    }




    public class SqlSelectBuilder<TIn, TOut, TWin> : ISqlSelectBuilder<TIn, TOut, TWin>
    {
        public SqlSelectBuilder(SelectClause<TIn, TOut, TWin> clause)
        {

            Clause = clause;
        }

        public SelectClause<TIn, TOut, TWin> Clause { get; }
        ISelectClause ISqlSelectHasClause.Clause => Clause;

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

    public interface ISelectClause
    {
        IFromListItem From { get; }
        SelectType Type { get; }
        IReadOnlyList<LambdaExpression> DistinctOn { get; }
        IWindowClauses Window { get; }

        LambdaExpression Select { get; }
        LambdaExpression Where { get; }
        int? Limit { get; }
        IReadOnlyList<IGroupByExpr> GroupBy { get; }
        IReadOnlyList<IOrderByExpr> OrderBy { get; }
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


    public class SelectClause : ISelectClause
    {
        public SelectClause(LambdaExpression select, LambdaExpression where, int? limit, IReadOnlyList<IGroupByExpr> groupBy, IReadOnlyList<IOrderByExpr> orderBy, IWindowClauses window, IFromListItem from, SelectType type, IReadOnlyList<LambdaExpression> distinctOn)
        {
            if (select == null)
                throw new ArgumentNullException(nameof(select));
            Select = select;
            Where = where;
            Limit = limit;
            GroupBy = groupBy;
            OrderBy = orderBy;
            Window = window;
            From = from;
            Type = type;
            DistinctOn = distinctOn;
        }

        public LambdaExpression Select { get; }
        public LambdaExpression Where { get; }
        public int? Limit { get; }
        public IReadOnlyList<IGroupByExpr> GroupBy { get; }
        public IReadOnlyList<IOrderByExpr> OrderBy { get; }
        public IWindowClauses Window { get; }
        public IFromListItem From { get; }
        public SelectType Type { get; }
        public IReadOnlyList<LambdaExpression> DistinctOn { get; }


    }

    /// <summary>
    /// Una clausula de SELECT
    /// </summary>
    public class SelectClause<TIn, TOut, TWin> : ISelectClause
    {
        public SelectClause(
            IFromListItem<TIn> from, SelectType type, IReadOnlyList<Expression<Func<TIn, object>>> distinctOn,
            WindowClauses<TWin> window, Expression<Func<TIn, TWin, TOut>> select,
            Expression<Func<TIn, TWin, bool>> where, IReadOnlyList<GroupByExpr<TIn>> groupBy, IReadOnlyList<OrderByExpr<TIn>> orderBy,
            int? limit)
        {
            From = from;
            Type = type;
            DistinctOn = distinctOn;
            Window = window;
            Select = select;
            Where = where;
            GroupBy = groupBy;
            OrderBy = orderBy;
            Limit = limit;
        }

        public SelectClause<TIn, TOut, TWin> SetSelect<TOut>(Expression<Func<TIn, TOut>> select) =>
           this.SetSelect(ExprHelper.AddParam<TIn, TWin, TOut>(select));

        public SelectClause<TIn, TOut, TWin> SetSelect<TOut>(Expression<Func<TIn, TWin, TOut>> select) =>
            new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Window, select, null, new GroupByExpr<TIn>[0], new OrderByExpr<TIn>[0], null);

        public SelectClause<TIn, TIn, TWin> SetFrom<TOut>(IFromListItem<TIn> from) =>
            new SelectClause<TIn, TIn, TWin>(from, Type, DistinctOn, Window, (x, win) => x, null, null, null, null);

        public SelectClause<TIn, TIn, TWinOut> SetWindow<TWinOut>(WindowClauses<TWinOut> window) =>
           new SelectClause<TIn, TIn, TWinOut>(From, Type, DistinctOn, window, (x, win) => x, null, null, null, null);

        public SelectClause<TIn, TIn, TWin> SetType(SelectType type) =>
           new SelectClause<TIn, TIn, TWin>(From, type, DistinctOn, Window, (x, win) => x, null, null, null, null);

        /// <summary>
        /// Establece la expresión del DISTINCT ON y el tipo del select
        /// </summary>
        /// <param name="distinctOn"></param>
        /// <returns></returns>
        public SelectClause<TIn, TOut, TWin> SetDistinctOn(IReadOnlyList<Expression<Func<TIn, object>>> distinctOn) =>
           new SelectClause<TIn, TOut, TWin>(From, SelectType.DistinctOn, distinctOn, Window, null, null, null, null, null);

        public SelectClause<TIn, TOut, TWin> AddDistinctOn(Expression<Func<TIn, object>> distinctOn) => SetDistinctOn(this.DistinctOn.Concat(new[] { distinctOn }).ToList());


        public SelectClause<TIn, TOut, TWin> SetOrderBy(IReadOnlyList<OrderByExpr<TIn>> orderBy) =>
                new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Window, Select, Where, GroupBy, orderBy, Limit);

        public SelectClause<TIn, TOut, TWin> AddOrderBy(OrderByExpr<TIn> item) => SetOrderBy(this.OrderBy.Concat(new[] { item }).ToList());

        public SelectClause<TIn, TOut, TWin> SetGroupBy(IReadOnlyList<GroupByExpr<TIn>> groupBy) =>
            new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Window, Select, Where, groupBy, OrderBy, Limit);

        public SelectClause<TIn, TOut, TWin> AddGroupBy(GroupByExpr<TIn> item) => SetGroupBy(this.GroupBy.Concat(new[] { item }).ToList());

        public SelectClause<TIn, TOut, TWin> AndWhere(Expression<Func<TIn, bool>> where) =>
            this.AndWhere(ExprHelper.AddParam<TIn, TWin, bool>(where));

        static Expression<Func<TIn, TWin, bool>> AndWhereExpr(Expression<Func<TIn, TWin, bool>> a, Expression<Func<TIn, TWin, bool>> b)
        {
            if (a == null) return b;
            var aBody = ReplaceVisitor.Replace(a.Body, new Dictionary<Expression, Expression>
            {
               { a.Parameters[0], b.Parameters[0]},
               { a.Parameters[1], b.Parameters[1] }
            });

            var body = Expression.AndAlso(aBody, b.Body);
            return Expression.Lambda<Func<TIn, TWin, bool>>(body, b.Parameters);
        }

        public SelectClause<TIn, TOut, TWin> AndWhere(Expression<Func<TIn, TWin, bool>> where) =>
            new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Window, Select, AndWhereExpr(this.Where, where), GroupBy, OrderBy, Limit);

        public SelectClause<TIn, TOut, TWin> SetWindow(Expression<Func<TIn, TWin, bool>> where) =>
          new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Window, Select, where, GroupBy, OrderBy, Limit);

        public SelectClause<TIn, TOut, TWin> SetLimit(int? limit) =>
          new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Window, Select, Where, GroupBy, OrderBy, limit);

        public SelectClause<TIn, TOut, TWin> SetWith(WithSelectClause with) =>
         new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Window, Select, Where, GroupBy, OrderBy, Limit);

        public IFromListItem<TIn> From { get; }
        public SelectType Type { get; }
        public IReadOnlyList<Expression<Func<TIn, object>>> DistinctOn { get; }
        IFromListItem ISelectClause.From => From;
        IReadOnlyList<LambdaExpression> ISelectClause.DistinctOn => DistinctOn;

        public WindowClauses<TWin> Window { get; }
        IWindowClauses ISelectClause.Window => Window;

        public Expression<Func<TIn, TWin, TOut>> Select { get; }
        public Expression<Func<TIn, TWin, bool>> Where { get; }
        public IReadOnlyList<GroupByExpr<TIn>> GroupBy { get; }
        public IReadOnlyList<OrderByExpr<TIn>> OrderBy { get; }

        public int? Limit { get; }

        LambdaExpression ISelectClause.Select => Select;
        LambdaExpression ISelectClause.Where => Where;
        IReadOnlyList<IGroupByExpr> ISelectClause.GroupBy => GroupBy;
        IReadOnlyList<IOrderByExpr> ISelectClause.OrderBy => OrderBy;
    }
}
