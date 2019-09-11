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

    /// <summary>
    /// Interfaz base para las interfaces que construyen un SELECT donde ya se definio el tipo WINDOW pero aún no se define el tipo de 
    /// retorno
    /// </summary>
    public interface ISqlSelectHasClause<TIn, TWin> : ISqlSelect<TIn>, ISqlSelectHasClause
    {
        SelectClause<TIn, TIn, TWin> Clause { get; }
    }

    /// <summary>
    /// Interfaz base para las interfaces que construyen un SELECT donde esta definido sólo el tipo de entra
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
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
