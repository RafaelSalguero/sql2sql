using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeaSql.Fluent.Data;

namespace KeaSql.Fluent
{
    public interface ISqlSelectHasClause : ISqlSelect
    {
        ISelectClause Clause { get; }
    }

    public interface ISqlSelectHasClause<TIn, TOut, TWin> : ISqlSelect<TOut>, ISqlSelectHasClause
    {
        SelectClause<TIn, TOut, TWin> Clause { get; }
    }

    public interface ISqlSelectHasClause<TIn, TWin> : ISqlSelect<TIn>, ISqlSelectHasClause
    {
        SelectClause<TIn, TIn, TWin> Clause { get; }
    }

    public interface ISqlSelectHasClause<TIn> : ISqlSelect<TIn>, ISqlSelectHasClause
    {
        SelectClause<TIn, TIn, object> Clause { get; }
    }

    #region Con WINDOW y Out
    public interface ISqlLimitAble<TIn, TOut, TWin> : ISqlSelectHasClause<TIn, TOut, TWin> { }
    public interface ISqlOrderByThenByAble<TIn, TOut, TWin> : ISqlLimitAble<TIn, TOut, TWin> { }
    public interface ISqlOrderByAble<TIn, TOut, TWin> : ISqlLimitAble<TIn, TOut, TWin> { }

    public interface ISqlGroupByThenByAble<TIn, TOut, TWin> : ISqlOrderByAble<TIn, TOut, TWin> { }
    public interface ISqlGroupByAble<TIn, TOut, TWin> : ISqlOrderByAble<TIn, TOut, TWin> { }
    public interface ISqlWherable<TIn, TOut, TWin> : ISqlGroupByAble<TIn, TOut, TWin> { }
    #endregion

    public interface ISqlSelectAble<TIn, TWin> : ISqlWherable<TIn, TIn, TWin>
    {
    }
    public interface ISqlSelectAble<TIn> : ISqlSelect, ISqlWherable<TIn, TIn, object>
    {
    }


    public interface ISqlWindowAble<T, TWin> : ISqlSelectAble<T, TWin> { }

    public interface ISqlWindowAble<T> : ISqlSelectAble<T> { }


    public interface ISqlFirstWindowAble<T> : ISqlWindowAble<T> { }

    public interface ISqlDistinctAble<T> : ISqlFirstWindowAble<T> { }
    public interface ISqlDistinctOnAble<T> : ISqlFirstWindowAble<T> { }
    public interface ISqlDistinctOnThenByAble<T> : ISqlFirstWindowAble<T> { }
    public interface ISqlDistinctDistinctOnAble<T> : ISqlDistinctAble<T>, ISqlDistinctOnAble<T> { }


    public interface ISqlJoinAble<T> : ISqlDistinctDistinctOnAble<T> { }
}
