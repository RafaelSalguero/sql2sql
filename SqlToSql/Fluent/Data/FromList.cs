using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using SqlToSql.Fluent.Data;

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
        LambdaExpression Map { get; }
        LambdaExpression On { get; }
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
        LambdaExpression ISqlJoin.Map => Map;
        LambdaExpression ISqlJoin.On => On;
    }

    public interface ISqlFromListAlias : IFromListItem {
        IFromListItem From { get; }
        LambdaExpression Map { get; }
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
        LambdaExpression ISqlFromListAlias.Map => Map;
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

    public class PreSelectPreWinBuilder<TIn> : ISqlJoinAble<TIn>, IFromListJoinAble<TIn>
    {
        public PreSelectPreWinBuilder(PreSelectClause<TIn, object> clause)
        {
            Clause = clause;
        }

        public PreSelectClause<TIn,object> Clause { get; }
        IPreSelectClause IFromListWindow.Clause => Clause;
    }

    public class PreSelectBuilder<TIn, TWin> : ISqlWindowAble<TIn, TWin>
    {
        public PreSelectBuilder(PreSelectClause<TIn, TWin> clause)
        {
            Clause = clause;
        }

        public PreSelectClause<TIn, TWin> Clause { get; }
        IPreSelectClause IFromListWindow.Clause => Clause;
    }


}
