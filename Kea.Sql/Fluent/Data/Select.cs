using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using KeaSql.ExprTree;

namespace KeaSql.Fluent.Data
{

    public interface ISqlPreSelectBuilder<TIn, TWin> :
          ISqlSelectAble<TIn, TWin>, ISqlWindowAble<TIn, TWin>
    {

    }

    public interface ISqlSelectBuilder<TIn, TOut, TWin> :
         ISqlSelect<TIn, TOut, TWin>, ISqlOrderByThenByAble<TIn, TOut, TWin>, ISqlOrderByAble<TIn, TOut, TWin>, ISqlGroupByAble<TIn, TOut, TWin>,
         ISqlWherable<TIn, TOut, TWin>, ISqlGroupByThenByAble<TIn, TOut, TWin>
    {

    }

    public class SqlPreSelectBuilder<TIn, TWin> : ISqlPreSelectBuilder<TIn, TWin>
    {
        public SqlPreSelectBuilder(PreSelectClause<TIn, TWin> clause)
        {
            Clause = clause;
        }

        public PreSelectClause<TIn, TWin> Clause { get; }
        IPreSelectClause IFromListWindow.Clause => Clause;
    }

    public class SqlSelectBuilder<TIn, TOut, TWin> : ISqlSelectBuilder<TIn, TOut, TWin>
    {
        public SqlSelectBuilder(SelectClause<TIn, TOut, TWin> clause)
        {
            Clause = clause;
        }

        public SelectClause<TIn, TOut, TWin> Clause { get; }
        ISelectClause ISqlSelect.Clause => Clause;

        public override string ToString()
        {
            var dic = new SqlText.SqlParamDic();
            return SqlText.SqlSelect.SelectToString(Clause, SqlText.ParamMode.EntityFramework, dic);
        }
    }

    public enum SelectType
    {
        All,
        Distinct,
        DistinctOn
    }

    public interface IPreSelectPreWindowClause
    {
        IFromListItem From { get; }
        SelectType Type { get; }
        IReadOnlyList<LambdaExpression> DistinctOn { get; }
    }

    public interface IPreSelectPreWindowClause<TIn> : IPreSelectPreWindowClause
    {
        IFromListItem<TIn> From { get; }
        SelectType Type { get; }
        IReadOnlyList<Expression<Func<TIn, object>>> DistinctOn { get; }
    }

    public interface IPreSelectClause : IPreSelectPreWindowClause
    {
        IWindowClauses Window { get; }
    }

    public interface IPreSelectClause<TIn, TWin> : IPreSelectPreWindowClause<TIn>, IPreSelectClause
    {
        IFromListItem<TIn> From { get; }
        SelectType Type { get; }
        IReadOnlyList<Expression<Func<TIn, object>>> DistinctOn { get; }
        WindowClauses<TWin> Window { get; }
    }

    public class PreSelectClause<TIn, TWin> : IPreSelectClause<TIn, TWin>
    {
        public PreSelectClause(IFromListItem<TIn> from, SelectType type, IReadOnlyList<Expression<Func<TIn, object>>> distinctOn, WindowClauses<TWin> window)
        {
            From = from;
            Type = type;
            DistinctOn = distinctOn;
            Window = window;
        }

        public SelectClause<TIn, TOut, TWin> SetSelect<TOut>(Expression<Func<TIn, TOut>> select) =>
            this.SetSelect(ExprHelper.AddParam<TIn, TWin, TOut>(select));

        public SelectClause<TIn, TOut, TWin> SetSelect<TOut>(Expression<Func<TIn, TWin, TOut>> select) =>
            new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, select, null, new GroupByExpr<TIn>[0], new OrderByExpr<TIn>[0], null, Window, null);

        public PreSelectClause<TIn, TWin> SetFrom<TOut>(IFromListItem<TIn> from) =>
            new PreSelectClause<TIn, TWin>(from, Type, DistinctOn, Window);

        public PreSelectClause<TIn, TWinOut> SetWindow<TWinOut>(WindowClauses<TWinOut> window) =>
           new PreSelectClause<TIn, TWinOut>(From, Type, DistinctOn, window);

        public PreSelectClause<TIn, TWin> SetType(SelectType type) =>
           new PreSelectClause<TIn, TWin>(From, type, DistinctOn, Window);

        /// <summary>
        /// Establece la expresión del DISTINCT ON y el tipo del select
        /// </summary>
        /// <param name="distinctOn"></param>
        /// <returns></returns>
        public PreSelectClause<TIn, TWin> SetDistinctOn(IReadOnlyList<Expression<Func<TIn, object>>> distinctOn) =>
           new PreSelectClause<TIn, TWin>(From, SelectType.DistinctOn, distinctOn, Window);

        public PreSelectClause<TIn, TWin> AddDistinctOn(Expression<Func<TIn, object>> distinctOn) => SetDistinctOn(this.DistinctOn.Concat(new[] { distinctOn }).ToList());


        public IFromListItem<TIn> From { get; }
        public SelectType Type { get; }
        public IReadOnlyList<Expression<Func<TIn, object>>> DistinctOn { get; }
        IFromListItem IPreSelectPreWindowClause.From => From;
        IReadOnlyList<LambdaExpression> IPreSelectPreWindowClause.DistinctOn => DistinctOn;

        public WindowClauses<TWin> Window { get; }
        IWindowClauses IPreSelectClause.Window => Window;

    }

    public interface ISelectClause : IPreSelectClause
    {
        WithSelectClause With { get; }
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
        public SelectClause(LambdaExpression select, LambdaExpression where, int? limit, IReadOnlyList<IGroupByExpr> groupBy, IReadOnlyList<IOrderByExpr> orderBy, IWindowClauses window, IFromListItem from, SelectType type, IReadOnlyList<LambdaExpression> distinctOn, WithSelectClause with)
        {
            Select = select;
            Where = where;
            Limit = limit;
            GroupBy = groupBy;
            OrderBy = orderBy;
            Window = window;
            From = from;
            Type = type;
            DistinctOn = distinctOn;
            With = with;
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
        public WithSelectClause With { get; }

        
    }

    /// <summary>
    /// Una clausula de SELECT
    /// </summary>
    public class SelectClause<TIn, TOut, TWin> : PreSelectClause<TIn, TWin>, ISelectClause
    {
        public SelectClause(
            IFromListItem<TIn> from, SelectType type, IReadOnlyList<Expression<Func<TIn, object>>> distinctOn,
            Expression<Func<TIn, TWin, TOut>> select, Expression<Func<TIn, TWin, bool>> where,
             IReadOnlyList<GroupByExpr<TIn>> groupBy, IReadOnlyList<OrderByExpr<TIn>> orderBy, int? limit,
            WindowClauses<TWin> window, WithSelectClause with
            ) : base(from, type, distinctOn, window)
        {
            Select = select;
            Where = where;
            GroupBy = groupBy;
            OrderBy = orderBy;
            Limit = limit;
            With = with;
        }

        public SelectClause<TIn, TOut, TWin> SetOrderBy(IReadOnlyList<OrderByExpr<TIn>> orderBy) =>
            new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Select, Where, GroupBy, orderBy, Limit, Window, With);

        public SelectClause<TIn, TOut, TWin> AddOrderBy(OrderByExpr<TIn> item) => SetOrderBy(this.OrderBy.Concat(new[] { item }).ToList());

        public SelectClause<TIn, TOut, TWin> SetGroupBy(IReadOnlyList<GroupByExpr<TIn>> groupBy) =>
            new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Select, Where, groupBy, OrderBy, Limit, Window, With);

        public SelectClause<TIn, TOut, TWin> AddGroupBy(GroupByExpr<TIn> item) => SetGroupBy(this.GroupBy.Concat(new[] { item }).ToList());

        public SelectClause<TIn, TOut, TWin> SetWhere(Expression<Func<TIn, bool>> where) =>
            this.SetWhere(ExprHelper.AddParam<TIn, TWin, bool>(where));

        public SelectClause<TIn, TOut, TWin> SetWhere(Expression<Func<TIn, TWin, bool>> where) =>
            new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Select, where, GroupBy, OrderBy, Limit, Window, With);

        public SelectClause<TIn, TOut, TWin> SetWindow(Expression<Func<TIn, TWin, bool>> where) =>
          new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Select, where, GroupBy, OrderBy, Limit, Window, With);

        public SelectClause<TIn, TOut, TWin> SetLimit(int? limit) =>
          new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Select, Where, GroupBy, OrderBy, limit, Window, With);

        public SelectClause<TIn, TOut, TWin> SetWith(WithSelectClause with) =>
         new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Select, Where, GroupBy, OrderBy, Limit, Window, with);

        public Expression<Func<TIn, TWin, TOut>> Select { get; }
        public Expression<Func<TIn, TWin, bool>> Where { get; }
        public IReadOnlyList<GroupByExpr<TIn>> GroupBy { get; }
        public IReadOnlyList<OrderByExpr<TIn>> OrderBy { get; }

        public int? Limit { get; }

        LambdaExpression ISelectClause.Select => Select;
        LambdaExpression ISelectClause.Where => Where;
        IReadOnlyList<IGroupByExpr> ISelectClause.GroupBy => GroupBy;
        IReadOnlyList<IOrderByExpr> ISelectClause.OrderBy => OrderBy;
        public WithSelectClause With { get; }
    }
}
