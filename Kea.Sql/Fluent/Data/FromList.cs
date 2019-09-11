﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using KeaSql.Fluent.Data;

namespace KeaSql.Fluent
{
    public interface IFromListItem   { }
    public interface IFromListItem<T> : IFromListItem { }


    public interface IFromListItemTarget { }
    public interface IFromListItemTarget<out T> : IFromListItemTarget { }

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

    public interface ISqlTableRefRaw {
        string Raw { get; }
    }

    public class SqlTableRefRaw<T> : IFromListItemTarget<T>, ISqlTableRefRaw
    {
        public SqlTableRefRaw(string raw)
        {
            Raw = raw;
        }

        public string Raw { get; }
    }

    public interface ISqlSelectRaw
    {
        string Raw { get; }
    }

    /// <summary>
    /// Un SELECT definido por el SQL Raw
    /// </summary>
    public class SqlSelectRaw<T> : ISqlSelect<T>, ISqlSelectRaw
    {
        public SqlSelectRaw(string raw)
        {
            Raw = raw;
        }

        public string Raw { get; }
        public override string ToString()
        {
            return Raw;
        }
    }


    public interface ISqlFrom : IFromListItem
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

    public interface ISqlFromWin : IFromListItem
    {
        ISqlWith Left { get; }
        LambdaExpression Target { get; }
    }
    public interface ISqlFromWin<TIn, TOut> : IFromListItem<TOut>
    {
        ISqlWith<TIn> Left { get; }
        Expression<Func<TIn, IFromListItemTarget<TOut>>> Target { get; }
    }

    public class SqlFrom<TIn, TOut> : ISqlFromWin<TIn, TOut>
    {
        public SqlFrom(ISqlWith<TIn> left, Expression<Func<TIn, IFromListItemTarget<TOut>>> target)
        {
            Left = left;
            Target = target;
        }

        public ISqlWith<TIn> Left { get; }
        Expression<Func<TIn, IFromListItemTarget<TOut>>> Target { get; }
        Expression<Func<TIn, IFromListItemTarget<TOut>>> ISqlFromWin<TIn, TOut>.Target => Target;
    }

    public interface ISqlJoin : IFromListItem
    {
        JoinType Type { get; }
        bool Lateral { get; }
        IFromListItem Left { get; }
        LambdaExpression Right { get; }
        LambdaExpression Map { get; }
        LambdaExpression On { get; }
    }


    public interface ISqlJoin<TRet> : IFromListItem<TRet>, ISqlJoin
    {
        Expression<Func<TRet, bool>> On { get; }
    }

    public enum JoinType
    {
        From,
        With,
        Inner,
        Left,
        Right,
        Outter,
        Cross,
    }

    public class SqlJoin<T1, T2, TRet> : ISqlJoin<TRet>
    {
        public SqlJoin(IFromListItem<T1> left, Expression<Func<T1, IFromListItemTarget<T2>>> right, Expression<Func<T1, T2, TRet>> map, Expression<Func<TRet, bool>> on, JoinType type, bool lateral)
        {
            Left = left;
            Right = right;
            Map = map;
            On = on;
            Type = type;
            Lateral = lateral;
        }

        public IFromListItem<T1> Left { get; }
        public Expression<Func<T1, IFromListItemTarget<T2>>> Right { get; }
        public Expression<Func<T1, T2, TRet>> Map { get; }
        public Expression<Func<TRet, bool>> On { get; }
        public JoinType Type { get; }
        public bool Lateral { get; }

        IFromListItem ISqlJoin.Left => Left;
        LambdaExpression ISqlJoin.Right => Right;
        LambdaExpression ISqlJoin.Map => Map;
        LambdaExpression ISqlJoin.On => On;
    }

    public interface ISqlFromListAlias : IFromListItem
    {
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

    public interface IJoinTypeAble<T> : ISqlJoinAble<T, T, object>
    { }

    public interface IJoinLateralAble<TL>
    {
        JoinType Type { get; }
        ISqlJoinAble<TL, TL, object> Left { get; }
        bool Lateral { get; }

        IJoinOnAble<TL, TR> JoinTable<TR>(string table);
        IJoinOnAble<TL, TR> JoinTable<TR>();
    }
    public interface IJoinOnAble<TL, TR> : IJoinLateralAble<TL>
    {
        Expression<Func<TL, IFromListItemTarget<TR>>> Right { get; }
    }

    public interface IJoinOnTupleAble<TL, TR> : IJoinOnAble<TL, TR> { }
    public interface IJoinOnMapAble<TL, TR> : IJoinOnAble<TL, TR> { }

    public class JoinItems<TL, TR> : IJoinLateralAble<TL>, IJoinOnTupleAble<TL, TR>, IJoinOnMapAble<TL, TR>
    {
        public JoinItems(JoinType type, bool lateral, ISqlJoinAble<TL, TL, object> left, Expression<Func<TL, IFromListItemTarget<TR>>> right)
        {
            Type = type;
            Lateral = lateral;
            Left = left;
            Right = right;
        }

        public JoinType Type { get; }
        public bool Lateral { get; }
        public ISqlJoinAble<TL, TL, object> Left { get; }
        public Expression<Func<TL, IFromListItemTarget<TR>>> Right { get; }

        public IJoinOnAble<TL, TR1> JoinTable<TR1>(string table) => this.Join(new SqlTable<TR1>(table));
        public IJoinOnAble<TL, TR1> JoinTable<TR1>() => this.Join(new SqlTable<TR1>());
    }
}
