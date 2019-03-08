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
    public interface ISqlSelect<TIn, TOut, TWin> : IFromListItemTarget<TOut>, ISqlSelect
    {
        SelectClause<TIn, TOut, TWin> Clause { get; }
    }

    public interface ISqlLimitAble<TIn, TOut, TWin> : ISqlSelect<TIn, TOut, TWin> { }
    public interface ISqlOrderByThenByAble<TIn, TOut, TWin> : ISqlLimitAble<TIn, TOut, TWin> { }
    public interface ISqlOrderByAble<TIn, TOut, TWin> : ISqlLimitAble<TIn, TOut, TWin> { }

    public interface ISqlGroupByAble<TIn, TOut, TWin> : ISqlOrderByAble<TIn, TOut, TWin> { }
    public interface ISqlWherable<TIn, TOut, TWin> : ISqlGroupByAble<TIn, TOut, TWin> { }

    public interface IFromList<T>
    {
        PreSelectClause<T> Clause { get; }
    }

    public interface IFromListJoinAble<T> : IFromList<T> { }

    public interface ISqlSelectAble<T>  : IFromList<T>
    {
    }

    public interface ISqlDistinctAble<T> : ISqlSelectAble<T> { }
    public interface ISqlDistinctOnAble<T> : ISqlSelectAble<T> { }
    public interface ISqlDistinctDistinctOnAble<T> : ISqlDistinctAble<T>, ISqlDistinctOnAble<T> { }


    public interface ISqlJoinAble<T> : ISqlDistinctDistinctOnAble<T> { }
}
