using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlToSql.Fluent
{
    public interface ISqlSelect<TIn, TOut> : IFromListItem<TOut>
    {
        SelectClause<TIn, TOut> Clause { get; }
    }

    public interface ISqlLimitAble<TIn, TOut> : ISqlSelect<TIn, TOut> { }
    public interface ISqlOrderByThenByAble<TIn, TOut> : ISqlLimitAble<TIn, TOut> { }
    public interface ISqlOrderByAble<TIn, TOut> : ISqlLimitAble<TIn, TOut> { }

    public interface ISqlGroupByAble<TIn, TOut> : ISqlOrderByAble<TIn, TOut> { }
    public interface ISqlWherable<TIn, TOut> : ISqlGroupByAble<TIn, TOut> { }


    public interface ISqlSelectAble<T>
    {
        PreSelectClause<T> Clause { get; }
    }

    public interface ISqlDistinctAble<T> : ISqlSelectAble<T> { }
    public interface ISqlDistinctOnAble<T> : ISqlSelectAble<T> { }
    public interface ISqlDistinctDistinctOnAble<T> : ISqlDistinctAble<T>, SqlDistinctOnAble<T> { }


    public interface ISqlJoinAble<T> : ISqlDistinctDistinctOnAble<T> { }
}
