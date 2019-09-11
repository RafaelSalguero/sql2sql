using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeaSql.Fluent.Data;

namespace KeaSql.Fluent
{
    /// <summary>
    /// Interfaz base de todas las interfaces que construyen a un SELECT
    /// </summary>
    public interface ISqlSelectHasClause : ISqlSelect
    {
        ISelectClause Clause { get; }
    }

    /// <summary>
    /// Interfaz base para las interfaces que construyen un SELECT donde ya esta definido el tipo de retorno y el tipo del WINDOW
    /// </summary>
    public interface ISqlSelectHasClause<TIn, TOut, TWin> : ISqlSelect<TOut>, ISqlSelectHasClause
    {
        SelectClause<TIn, TOut, TWin> Clause { get; }
    }

    #region Con WINDOW y Out
    public interface ISqlLimitAble<TIn, TOut, TWin> : ISqlSelectHasClause<TIn, TOut, TWin> { }
    public interface ISqlOrderByThenByAble<TIn, TOut, TWin> : ISqlLimitAble<TIn, TOut, TWin> { }
    public interface ISqlOrderByAble<TIn, TOut, TWin> : ISqlLimitAble<TIn, TOut, TWin> { }

    public interface ISqlGroupByThenByAble<TIn, TOut, TWin> : ISqlOrderByAble<TIn, TOut, TWin> { }
    public interface ISqlGroupByAble<TIn, TOut, TWin> : ISqlOrderByAble<TIn, TOut, TWin> { }
    public interface ISqlWherable<TIn, TOut, TWin> : ISqlGroupByAble<TIn, TOut, TWin> { }
    #endregion

    public interface ISqlSelectAble<TIn, TOut, TWin> : ISqlWherable<TIn, TOut, TWin>
    {
    }
 


    public interface ISqlWindowAble<TIn, TOut, TWin> : ISqlSelectAble<TIn, TOut, TWin> { }

    public interface ISqlFirstWindowAble<TIn, TOut, TWin> : ISqlWindowAble<TIn, TOut, TWin> { }

    public interface ISqlDistinctAble<TIn, TOut, TWin> : ISqlFirstWindowAble<TIn, TOut, TWin> { }
    public interface ISqlDistinctOnAble<TIn, TOut, TWin> : ISqlFirstWindowAble<TIn, TOut, TWin> { }
    public interface ISqlDistinctOnThenByAble<TIn, TOut, TWin> : ISqlFirstWindowAble<TIn, TOut, TWin> { }
    public interface ISqlDistinctDistinctOnAble<TIn, TOut, TWin> : ISqlDistinctAble<TIn, TOut, TWin>, ISqlDistinctOnAble<TIn, TOut, TWin> { }


    public interface ISqlJoinAble<TIn, TOut, TWin> : ISqlDistinctDistinctOnAble<TIn, TOut, TWin> { }
}
