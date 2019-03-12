using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using SqlToSql.ExprTree;

namespace SqlToSql.Fluent.Data
{

    public interface ISqlPreSelectBuilder<TIn, TWin> :
          ISqlSelectAble<TIn, TWin>, ISqlWindowAble<TIn, TWin>
    {

    }

    public interface ISqlSelectBuilder<TIn, TOut, TWin> :
         ISqlSelect<TIn, TOut, TWin>, ISqlOrderByThenByAble<TIn, TOut, TWin>, ISqlOrderByAble<TIn, TOut, TWin>, ISqlGroupByAble<TIn, TOut, TWin>,
         ISqlWherable<TIn, TOut, TWin>
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
        LambdaExpression DistinctOn { get; }
    }

    public interface IPreSelectPreWindowClause<TIn> : IPreSelectPreWindowClause
    {
        IFromListItem<TIn> From { get; }
        SelectType Type { get; }
        Expression<Func<TIn, object>> DistinctOn { get; }
    }

    public interface IPreSelectClause : IPreSelectPreWindowClause
    {
        IWindowClauses Window { get; }
    }

    public interface IPreSelectClause<TIn, TWin> : IPreSelectPreWindowClause<TIn>, IPreSelectClause
    {
        IFromListItem<TIn> From { get; }
        SelectType Type { get; }
        Expression<Func<TIn, object>> DistinctOn { get; }
        WindowClauses<TWin> Window { get; }
    }

    public class PreSelectClause<TIn, TWin> : IPreSelectClause<TIn, TWin>
    {
        public PreSelectClause(IFromListItem<TIn> from, SelectType type, Expression<Func<TIn, object>> distinctOn, WindowClauses<TWin> window)
        {
            From = from;
            Type = type;
            DistinctOn = distinctOn;
            Window = window;
        }

        public SelectClause<TIn, TOut, TWin> SetSelect<TOut>(Expression<Func<TIn, TOut>> select) =>
            this.SetSelect(ExprHelper.AddParam<TIn, TWin, TOut>(select));

        public SelectClause<TIn, TOut, TWin> SetSelect<TOut>(Expression<Func<TIn,TWin, TOut>> select) =>
            new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, select, null, null, null, null, Window);

        public PreSelectClause<TIn,  TWin> SetFrom<TOut>(IFromListItem<TIn> from) =>
            new PreSelectClause<TIn,  TWin>(from, Type, DistinctOn, Window);

        public PreSelectClause<TIn, TWinOut> SetWindow<TWinOut>(WindowClauses<TWinOut> window) =>
           new PreSelectClause<TIn, TWinOut>(From, Type, DistinctOn, window);

        public IFromListItem<TIn> From { get; }
        public SelectType Type { get; }
        public Expression<Func<TIn, object>> DistinctOn { get; }
        IFromListItem IPreSelectPreWindowClause.From => From;
        LambdaExpression IPreSelectPreWindowClause.DistinctOn => DistinctOn;

        public WindowClauses<TWin> Window { get; }
        IWindowClauses IPreSelectClause.Window => Window;

    }

    public interface ISelectClause : IPreSelectClause
    {
        LambdaExpression Select { get; }
        LambdaExpression Where { get; }
        int? Limit { get; }
    IReadOnlyList<IGroupByExpr> GroupBy { get; }
        IReadOnlyList<IOrderByExpr> OrderBy { get; }
    }

    /// <summary>
    /// Una clausula de SELECT
    /// </summary>
    public class SelectClause<TIn, TOut, TWin> : PreSelectClause<TIn, TWin>, ISelectClause
    {
        public SelectClause(
            IFromListItem<TIn> from, SelectType type, Expression<Func<TIn, object>> distinctOn,
            Expression<Func<TIn, TWin, TOut>> select, Expression<Func<TIn, TWin, bool>> where,
             IReadOnlyList<GroupByExpr<TIn>> groupBy, IReadOnlyList<OrderByExpr<TIn>> orderBy, int? limit,
            WindowClauses<TWin> window
            ) : base(from, type, distinctOn, window)
        {
            Select = select;
            Where = where;
            GroupBy = groupBy;
            OrderBy = orderBy;
            Limit = limit;
        }

        public SelectClause<TIn, TOut, TWin> SetWhere(Expression<Func<TIn,   bool>> where) =>
            this.SetWhere( ExprHelper.AddParam<TIn, TWin, bool>(where));

        public SelectClause<TIn, TOut, TWin> SetWhere(Expression<Func<TIn, TWin, bool>> where) =>
            new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Select, where, GroupBy, OrderBy, Limit, Window);

        public SelectClause<TIn, TOut, TWin> SetWindow(Expression<Func<TIn, TWin, bool>> where) =>
          new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Select, where, GroupBy, OrderBy, Limit, Window);


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
