using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
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
            new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, select, null, null, null, null, null);

        public PreSelectClause<TIn, TWinOut> SetWindow<TWinOut>(WindowClauses<TWinOut> window) =>
           new PreSelectClause<TIn, TWinOut>(From, Type, DistinctOn, window);

        public IFromListItem<TIn> From { get; }
        public SelectType Type { get; }
        public Expression<Func<TIn, object>> DistinctOn { get; }
        public WindowClauses<TWin> Window { get; }

        IFromListItem IPreSelectPreWindowClause.From => From;
        LambdaExpression IPreSelectPreWindowClause.DistinctOn => DistinctOn;
        IWindowClauses IPreSelectClause.Window => Window;

    }

    public interface ISelectClause : IPreSelectClause
    {
        LambdaExpression Select { get; }
        LambdaExpression Where { get; }
        LambdaExpression GroupBy { get; }
        int? Limit { get; }
    }

    /// <summary>
    /// Una clausula de SELECT
    /// </summary>
    public class SelectClause<TIn, TOut, TWin> : PreSelectClause<TIn, TWin>, ISelectClause
    {
        public SelectClause(
            IFromListItem<TIn> from, SelectType type, Expression<Func<TIn, object>> distinctOn,
            Expression<Func<TIn, TOut>> select, Expression<Func<TIn, bool>> where,
            Expression<Func<TIn, object>> groupBy, IReadOnlyList<OrderByExpr<TIn>> orderBy, int? limit,
            WindowClauses<TWin> window
            ) : base(from, type, distinctOn, window)
        {
            Select = select;
            Where = where;
            GroupBy = groupBy;
            OrderBy = orderBy;
            Limit = limit;
        }

        public SelectClause<TIn, TOut, TWin> SetWhere(Expression<Func<TIn, bool>> where) =>
            new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Select, where, GroupBy, OrderBy, Limit, Window);

        public SelectClause<TIn, TOut, TWin> SetWindow(Expression<Func<TIn, bool>> where) =>
          new SelectClause<TIn, TOut, TWin>(From, Type, DistinctOn, Select, where, GroupBy, OrderBy, Limit, Window);


        public Expression<Func<TIn, TOut>> Select { get; }
        public Expression<Func<TIn, bool>> Where { get; }
        public Expression<Func<TIn, object>> GroupBy { get; }
        public IReadOnlyList<OrderByExpr<TIn>> OrderBy { get; }
        public int? Limit { get; }

        LambdaExpression ISelectClause.Select => Select;
        LambdaExpression ISelectClause.Where => Where;
        LambdaExpression ISelectClause.GroupBy => GroupBy;


    }
}
