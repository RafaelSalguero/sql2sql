using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlToSql.Fluent
{
    public interface ISqlSelect : IFromListItemTarget
    {
        ISelectClause Clause { get; }
    }
    public interface ISqlSelect<TIn, TOut> : IFromListItemTarget<TOut>, ISqlSelect
    {
        SelectClause<TIn, TOut> Clause { get; }
    }

    public interface ISqlLimitAble<TIn, TOut> : ISqlSelect<TIn, TOut> { }
    public interface ISqlOrderByThenByAble<TIn, TOut> : ISqlLimitAble<TIn, TOut> { }
    public interface ISqlOrderByAble<TIn, TOut> : ISqlLimitAble<TIn, TOut> { }

    public interface ISqlGroupByAble<TIn, TOut> : ISqlOrderByAble<TIn, TOut> { }
    public interface ISqlWherable<TIn, TOut> : ISqlGroupByAble<TIn, TOut> { }

    public interface IFromList<T>
    {
        PreSelectClause<T> Clause { get; }
    }

    public interface ISqlSelectAble<T> : IFromList<T>
    {
    }

    public interface ISqlDistinctAble<T> : ISqlSelectAble<T> { }
    public interface ISqlDistinctOnAble<T> : ISqlSelectAble<T> { }
    public interface ISqlDistinctDistinctOnAble<T> : ISqlDistinctAble<T>, ISqlDistinctOnAble<T> { }


    public interface ISqlJoinAble<T> : ISqlDistinctDistinctOnAble<T> { }
}
