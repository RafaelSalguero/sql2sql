using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Sql2Sql.Fluent.Data;

namespace Sql2Sql.Fluent
{
    public interface IFromListItem { }
    public interface IFromListItem<T> : IFromListItem { }


    public interface IFromListItemTarget : ISqlStatement { }
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
        public SqlTable() : this(null) { } 
        /// <summary>
        /// Tabla con el nombre especificado.
        /// </summary>
        /// <param name="name">Nombre de la tabla, si es null se toma del nombre se <typeparamref name="T"/></param>
        public SqlTable(string name) : base(name ?? typeof(T).Name) { }
    }

    public interface ISqlTableRefRaw
    {
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
        new IFromListItemTarget<T> Target { get; }
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
        new Expression<Func<TRet, bool>> On { get; }
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

    /// <summary>
    /// A JOIN clause
    /// </summary>
    public class SqlJoin : IFromListItem
    {
        public SqlJoin(IFromListItem left, LambdaExpression right, LambdaExpression map, LambdaExpression on, JoinType type, bool lateral)
        {
            Left = left;
            Right = right;
            Map = map;
            On = on;
            Type = type;
            Lateral = lateral;
        }

        /// <summary>
        /// The left side of the JOIN
        /// </summary>
        public IFromListItem Left { get; }

        /// <summary>
        /// An expression (left) => right
        /// 'left' type equals to the element type of the <see cref="Left"/> <see cref="IFromListItem"/>
        /// 'right' type is <see cref="IFromListItem"/> and represents the right side of the join
        /// </summary>
        public LambdaExpression Right { get; }
        /// <summary>
        /// An expression (l, r) => ret
        /// 'l' is a row of <see cref="Left"/>
        /// 'r' is a row of <see cref="Right"/>
        /// 'ret' is a name mapping of both 'l' and 'r'. Usually a constructor that contains both l and r
        /// </summary>
        public LambdaExpression Map { get; }

        /// <summary>
        /// An expression (ret) => on
        /// 'ret' type is the return type of <see cref="Map"/>
        /// 'on' is a boolean representing the expression of the ON part of the join
        /// </summary>
        public LambdaExpression On { get; }

        /// <summary>
        /// The type of the join
        /// </summary>
        public JoinType Type { get; }

        /// <summary>
        /// If the join is a lateral one
        /// </summary>
        public bool Lateral { get; }

    }

    
    /// <summary>
    /// A name alias for a FROM list, usually used after a series of JOINs
    /// </summary>
    public class FromListAlias : IFromListItem
    {
        public FromListAlias(IFromListItem  from, LambdaExpression map)
        {
            From = from;
            Map = map;
        }

        /// <summary>
        /// The last from list item, all previews from list items are linked to this one
        /// </summary>
        public IFromListItem From { get; }
        /// <summary>
        /// (in) => out
        /// 'in' type is the element type of <see cref="From"/> and represents a row of the from list
        /// 'out' type is the output type of the from list, and is a constructor representing on each property an aliasing for an original from list item property
        /// </summary>
        public LambdaExpression  Map { get; }
    }

    public interface IBaseLeftJoinAble<TL>
    {
        JoinType Type { get; }
        ISqlSelectHasClause<TL, TL, object> Left { get; }
        bool Lateral { get; }


    }
    public interface IBaseLeftRightJoinOnAble<TL, TR> : IBaseLeftJoinAble<TL>
    {
        Expression<Func<TL, IFromListItemTarget<TR>>> Right { get; }
    }

    /// <summary>
    /// An object that can emit a JOIN
    /// </summary>
    /// <typeparam name="TL">Type of the left side of the JOIN</typeparam>
    public interface IFirstJoinAble<TL>
    {
        /// <summary>
        /// A JOIN to the given table
        /// </summary>
        IFirstJoinOnAble<TL, TR> Join<TR>(string table);

        /// <summary>
        /// A JOIN to the given table
        /// </summary>
        IFirstJoinOnAble<TL, TR> Join<TR>();
    }

    /// <summary>
    /// An object that can emit a JOIN
    /// </summary>
    /// <typeparam name="TL">Type of the left side of the JOIN</typeparam>
    public interface INextJoinAble<TL>
    {
        /// <summary>
        /// A JOIN to the given table
        /// </summary>
        INextJoinOnAble<TL, TR> Join<TR>(string table);

        /// <summary>
        /// A JOIN to the given table
        /// </summary>
        INextJoinOnAble<TL, TR> Join<TR>();
    }

    public interface IJoinAble<TL> : IFirstJoinAble<TL>, INextJoinAble<TL> { }

    //NOTE:
    /*
     * First and next joins need to be splitted in different types because the On function
     * at the first join has a T left param, which would match with all other types from other On functions,
     * which have a first TupleN<TN...> param
     * */

    public interface IFirstJoinLateralAble<TL> : IBaseLeftJoinAble<TL>, IFirstJoinAble<TL> { }
    public interface INextJoinLateralAble<TL> : IBaseLeftJoinAble<TL>, INextJoinAble<TL> { }

    public interface IJoinMapAble<TL, TR> : IBaseLeftRightJoinOnAble<TL, TR> { }


    public interface INextJoinOnAble<TL, TR> : IBaseLeftRightJoinOnAble<TL, TR> { }
    public interface IFirstJoinOnAble<TL, TR> : IBaseLeftRightJoinOnAble<TL, TR> { }

    public class JoinItems<TL, TR> :
        IFirstJoinLateralAble<TL>,
        INextJoinLateralAble<TL>,
        INextJoinOnAble<TL, TR>,
        IFirstJoinOnAble<TL, TR>
    {
        public JoinItems(JoinType type, bool lateral, ISqlSelectHasClause<TL, TL, object> left, Expression<Func<TL, IFromListItemTarget<TR>>> right)
        {
            Type = type;
            Lateral = lateral;
            Left = left;
            Right = right;
        }

        public JoinType Type { get; }
        public bool Lateral { get; }
        public ISqlSelectHasClause<TL, TL, object> Left { get; }
        public Expression<Func<TL, IFromListItemTarget<TR>>> Right { get; }

        IFirstJoinOnAble<TL, TR1> IFirstJoinAble<TL>.Join<TR1>() =>
            SqlSelectExtensions.InternalJoin(this, new SqlTable<TR1>());

        IFirstJoinOnAble<TL, TR1> IFirstJoinAble<TL>.Join<TR1>(string table) =>
            SqlSelectExtensions.InternalJoin(this, new SqlTable<TR1>(table));

        INextJoinOnAble<TL, TR1> INextJoinAble<TL>.Join<TR1>() =>
         SqlSelectExtensions.InternalJoin(this, new SqlTable<TR1>());

        INextJoinOnAble<TL, TR1> INextJoinAble<TL>.Join<TR1>(string table) =>
            SqlSelectExtensions.InternalJoin(this, new SqlTable<TR1>(table));

    }
}
