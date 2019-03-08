using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SqlToSql.Fluent
{
    public interface IFromListItem { }
    public interface IFromListItem<T> : IFromListItem { }


    public interface IFromListItemTarget { }
    public interface IFromListItemTarget<T> : IFromListItemTarget { }

    public enum JoinType
    {
        Inner,
        Left,
        Right,
        Outter,
        Cross
    }

    public class SqlTable : IFromListItemTarget
    {
        public SqlTable(string name)
        {
            Name = name;
        }
        public string Name { get; }
    }

    /// <summary>
    /// Una tabla de SQL
    /// </summary>
    public class SqlTable<T> : SqlTable, IFromListItemTarget<T>
    {
        public SqlTable() : base(typeof(T).Name) { }
        public SqlTable(string name) : base(name)
        {
        }
    }

    public interface ISqlFrom: IFromListItem
    {
        IFromListItemTarget Target { get; }
    }
    public interface ISqlFrom<T> : ISqlFrom, IFromListItem<T>
    {
        IFromListItemTarget<T> Target { get; }
    }

    /// <summary>
    /// Un FROM
    /// </summary>
    public class SqlFrom<T> : ISqlFrom<T>
    {
        public SqlFrom(IFromListItemTarget<T> target)
        {
            Target = target;
        }

        public IFromListItemTarget<T> Target { get; }
        IFromListItemTarget ISqlFrom.Target => Target;
    }

    public interface ISqlJoin: IFromListItem
    {
        IFromListItem Left { get; }
        IFromListItemTarget Right { get; }
    }

    public interface ISqlJoin<TRet> : IFromListItem<TRet>, ISqlJoin
    {
        Expression<Func<TRet, bool>> On { get; }
    }
    public class SqlJoin<T1, T2, TRet> : ISqlJoin<TRet>
    {
        public SqlJoin(IFromListItem<T1> left, IFromListItemTarget<T2> right, Expression<Func<T1, T2, TRet>> map, Expression<Func<TRet, bool>> on)
        {
            Left = left;
            Right = right;
            Map = map;
            On = on;
        }

        public IFromListItem<T1> Left { get; }
        public IFromListItemTarget<T2> Right { get; }
        public Expression<Func<T1, T2, TRet>> Map { get; }
        public Expression<Func<TRet, bool>> On { get; }

        IFromListItem ISqlJoin.Left => Left;
        IFromListItemTarget ISqlJoin.Right => Right;
    }

    public interface ISqlFromListAlias : IFromListItem {
        IFromListItem From { get; }
    }
    public interface ISqlFromListAlias<TIn, TOut> : ISqlFromListAlias, IFromListItem<TOut>
    {
        IFromListItem<TIn> From { get; }
        Expression<Func<TIn, TOut>> Map { get; }
    }
    public class FromListAlias<TIn, TOut> : ISqlFromListAlias<TIn, TOut>
    {
        public FromListAlias(IFromListItem<TIn> from, Expression<Func<TIn, TOut>> map)
        {
            From = from;
            Map = map;
        }

        public IFromListItem<TIn> From { get; }
        public Expression<Func<TIn, TOut>> Map { get; }

        IFromListItem ISqlFromListAlias.From => From;
    }

    public class JoinItems<TL, TR>
    {
        public JoinItems(JoinType type, IFromList<TL> left, IFromListItemTarget<TR> right)
        {
            Type = type;
            Left = left;
            Right = right;
        }

        public JoinType Type { get; }
        public IFromList<TL> Left { get; }
        public IFromListItemTarget<TR> Right { get; }
    }

    public class FromListFrom<T> : ISqlJoinAble<T>
    {
        public FromListFrom(IFromListItem<T> from)
        {
            Clause = new PreSelectClause<T>(from, SelectType.All, null);
        }

        public PreSelectClause<T> Clause { get; }
    }

    public class FromListJoin<T>: ISqlJoinAble<T>
    {
        public FromListJoin(IFromListItem<T> from)
        {
            Clause = new PreSelectClause<T>(from, SelectType.All, null);
        }

        public PreSelectClause<T> Clause { get; }
    }

    public class SqlDistinctOn<T> : ISqlSelectAble<T>
    {
        public SqlDistinctOn(PreSelectClause<T> select, Expression<Func<T, object>> distinctOn)
        {
            Clause = new PreSelectClause<T>(select.From, SelectType.DistinctOn, distinctOn);
        }

        public PreSelectClause<T> Clause { get; }
    }

    public class SqlDistinct<T> : ISqlSelectAble<T>
    {
        public SqlDistinct(PreSelectClause<T> select)
        {
            Clause = new PreSelectClause<T>(select.From, SelectType.Distinct, select.DistinctOn);
        }

        public PreSelectClause<T> Clause { get; }
    }

    public class SqlSelect<TIn, TOut> : ISqlWherable<TIn, TOut>
    {
        public SqlSelect(PreSelectClause<TIn> select, Expression<Func<TIn, TOut>> map)
        {
            Clause = new SelectClause<TIn, TOut>(select.From, select.Type, select.DistinctOn, map, null, null, null, null);
        }

        public SelectClause<TIn, TOut> Clause { get; }
        ISelectClause ISqlSelect.Clause => Clause;
    }

    public class SqlWhere<TIn, TOut> : ISqlGroupByAble<TIn, TOut>
    {
        public SqlWhere(SelectClause<TIn, TOut> select, Expression<Func<TIn, bool>> where)
        {
            this.Clause = new SelectClause<TIn, TOut>(select.From, select.Type, select.DistinctOn, select.Select, where, select.GroupBy, select.OrderBy, select.Limit);
        }
        public SelectClause<TIn, TOut> Clause { get; }
        ISelectClause ISqlSelect.Clause => Clause;
    }

    public class SqlGroupBy<TIn, TOut> : ISqlOrderByAble<TIn, TOut>
    {
        public SqlGroupBy(SelectClause<TIn, TOut> select, Expression<Func<TIn, object>> groupBy)
        {
            this.Clause = new SelectClause<TIn, TOut>(select.From, select.Type, select.DistinctOn, select.Select, select.Where, groupBy, select.OrderBy, select.Limit);
        }
        public SelectClause<TIn, TOut> Clause { get; }
        ISelectClause ISqlSelect.Clause => Clause;
    }

    public enum OrderByOrder
    {
        Asc,
        Desc
    }
    public enum OrderByNulls
    {
        NullsFirst,
        NullsLast
    }

    public class OrderByExpr<TIn>
    {
        public OrderByExpr(Expression<Func<TIn, object>> expr, OrderByOrder order, OrderByNulls? nulls)
        {
            Expr = expr;
            Order = order;
            Nulls = nulls;
        }

        public Expression<Func<TIn, object>> Expr { get; }
        public OrderByOrder Order { get; }
        public OrderByNulls? Nulls { get; }
    }

    public class SqlOrderBy<TIn, TOut> : ISqlOrderByThenByAble<TIn, TOut>
    {
        public SqlOrderBy(SelectClause<TIn, TOut> select, OrderByExpr<TIn> orderBy)
        {
            var list = (select.OrderBy ?? new OrderByExpr<TIn>[0]).Concat(new[] { orderBy }).ToList();
            this.Clause = new SelectClause<TIn, TOut>(select.From, select.Type, select.DistinctOn, select.Select, select.Where, select.GroupBy, list, select.Limit);
        }
        public SelectClause<TIn, TOut> Clause { get; }
        ISelectClause ISqlSelect.Clause => Clause;
    }

    public class SqlLimit<TIn, TOut> : ISqlSelect<TIn, TOut>
    {
        public SqlLimit(SelectClause<TIn, TOut> select, int limit)
        {
            this.Clause = new SelectClause<TIn, TOut>(select.From, select.Type, select.DistinctOn, select.Select, select.Where, select.GroupBy, select.OrderBy, limit);
        }
        public SelectClause<TIn, TOut> Clause { get; }
        ISelectClause ISqlSelect.Clause => Clause;
    }

    public enum SelectType
    {
        All,
        Distinct,
        DistinctOn
    }

    public class PreSelectClause<TIn>
    {
        public PreSelectClause(IFromListItem<TIn> from, SelectType type, Expression<Func<TIn, object>> distinctOn)
        {
            From = from;
            Type = type;
            DistinctOn = distinctOn;
        }

        public IFromListItem<TIn> From { get; }
        public SelectType Type { get; }
        public Expression<Func<TIn, object>> DistinctOn { get; }

    }

    public interface ISelectClause { }

    /// <summary>
    /// Una clausula de SELECT
    /// </summary>
    public class SelectClause<TIn, TOut> : PreSelectClause<TIn> , ISelectClause
    {
        public SelectClause(
            IFromListItem<TIn> from, SelectType type, Expression<Func<TIn, object>> distinctOn,
            Expression<Func<TIn, TOut>> select, Expression<Func<TIn, bool>> where, Expression<Func<TIn, object>> groupBy, IReadOnlyList<OrderByExpr<TIn>> orderBy, int? limit) : base(from, type, distinctOn)
        {
            Select = select;
            Where = where;
            GroupBy = groupBy;
            OrderBy = orderBy;
            Limit = limit;
        }

        public Expression<Func<TIn, TOut>> Select { get; }
        public Expression<Func<TIn, bool>> Where { get; }
        public Expression<Func<TIn, object>> GroupBy { get; }
        public IReadOnlyList<OrderByExpr<TIn>> OrderBy { get; }
        public int? Limit { get; }

    }
}
