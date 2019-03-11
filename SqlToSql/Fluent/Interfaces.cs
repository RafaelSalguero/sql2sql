using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlToSql.Fluent.Data;

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
        PreSelectClause<T, object> Clause { get; }
    }

    public interface IFromListWindow 
    {
        IPreSelectClause Clause { get; }
    }

    public interface IFromListWindow<T, TWin>: IFromListWindow
    {
        PreSelectClause<T, TWin> Clause { get; }
    }

    public interface IFromListJoinAble<T> : IFromList<T> { }

    public interface ISqlSelectAble<TIn, TWin>  : IFromListWindow<TIn, TWin>
    {
    }


    public interface ISqlWindowAble<T, TWin> : ISqlSelectAble<T, TWin> { }

    public interface ISqlDistinctAble<T> : ISqlWindowAble<T, object> { }
    public interface ISqlDistinctOnAble<T> : ISqlWindowAble<T, object> { }
    public interface ISqlDistinctDistinctOnAble<T> : ISqlDistinctAble<T>, ISqlDistinctOnAble<T> { }


    public interface ISqlJoinAble<T> : ISqlDistinctDistinctOnAble<T> { }
}
