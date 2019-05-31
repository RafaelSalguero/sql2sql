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
    /*
    #region Sin WINDOW, con Out
    public interface ISqlLimitAble<TIn, TOut> : ISqlSelectHasClause<TIn, TOut> { }
    public interface ISqlOrderByThenByAble<TIn, TOut> : ISqlLimitAble<TIn, TOut> { }
    public interface ISqlOrderByAble<TIn, TOut> : ISqlLimitAble<TIn, TOut> { }

    public interface ISqlGroupByThenByAble<TIn, TOut> : ISqlOrderByAble<TIn, TOut> { }
    public interface ISqlGroupByAble<TIn, TOut> : ISqlOrderByAble<TIn, TOut> { }
    public interface ISqlWherable<TIn, TOut> : ISqlGroupByAble<TIn, TOut> { }
    #endregion

    #region Sin WINDOW, sin Out
    public interface ISqlLimitAble<TIn> : ISqlSelectHasClause<TIn> { }
    public interface ISqlOrderByThenByAble<TIn> : ISqlLimitAble<TIn> { }
    public interface ISqlOrderByAble<TIn> : ISqlLimitAble<TIn> { }

    public interface ISqlGroupByThenByAble<TIn> : ISqlOrderByAble<TIn> { }
    public interface ISqlGroupByAble<TIn> : ISqlOrderByAble<TIn> { }
    public interface ISqlWherable<TIn> : ISqlGroupByAble<TIn> { }
    #endregion*/



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
