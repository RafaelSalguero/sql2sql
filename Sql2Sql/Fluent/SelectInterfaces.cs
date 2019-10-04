using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sql2Sql.Fluent.Data;

namespace Sql2Sql.Fluent
{
    /// <summary>
    /// Interfaz base de todas las interfaces que construyen a un SELECT
    /// </summary>
    public interface ISqlSelectHasClause : ISqlSelect
    {
        /// <summary>
        /// Cláusula del SELECT
        /// </summary>
        SelectClause Clause { get; }
    }

    /// <summary>
    /// Interfaz base para las interfaces que construyen un SELECT donde ya esta definido el tipo de retorno y el tipo del WINDOW
    /// </summary>
    public interface ISqlSelectHasClause<TIn, TOut, TWin> : ISqlSelect<TOut>, ISqlSelectHasClause { }

    /// <summary>
    /// LIMITE, va después del ORDER BY
    /// </summary>
    public interface ISqlLimitAble<TIn, TOut, TWin> : ISqlSelectHasClause<TIn, TOut, TWin> { }

    /// <summary>
    /// Expresiones extras del ORDER BY, va después de la primera expresión de ORDER BY
    /// </summary>
    public interface ISqlOrderByThenByAble<TIn, TOut, TWin> : ISqlLimitAble<TIn, TOut, TWin> { }

    /// <summary>
    /// Primera expresión de ORDER BY, va después del GROUP BY
    /// </summary>
    public interface ISqlOrderByAble<TIn, TOut, TWin> : ISqlLimitAble<TIn, TOut, TWin> { }

    /// <summary>
    /// Expresiones extras del GROUP BY, va después del GROUP BY
    /// </summary>
    public interface ISqlGroupByThenByAble<TIn, TOut, TWin> : ISqlOrderByAble<TIn, TOut, TWin> { }

    /// <summary>
    /// Primera expresión del GROUP BY, va después del WHERE
    /// </summary>
    public interface ISqlGroupByAble<TIn, TOut, TWin> : ISqlOrderByAble<TIn, TOut, TWin> { }

    /// <summary>
    /// WHERE, va después del SELECT
    /// </summary>
    public interface ISqlWherable<TIn, TOut, TWin> : ISqlGroupByAble<TIn, TOut, TWin> { }

    /// <summary>
    /// SELECT, va después del WINDOW (opcional), DISTINCT (opcional) o del FROM list
    /// </summary>
    public interface ISqlSelectAble<TIn, TOut, TWin> : ISqlWherable<TIn, TOut, TWin>
    {
    }

    /// <summary>
    /// WINDOW, va después del DISTINCT (opcional) o del FROM list
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    /// <typeparam name="TWin"></typeparam>
    public interface ISqlWindowAble<TIn, TOut, TWin> : ISqlSelectAble<TIn, TOut, TWin> { }

    /// <summary>
    /// Expresiones adicionales del DISTINCT ON, van después del primer DISTINCT ON
    /// </summary>
    public interface ISqlDistinctOnThenByAble<TIn, TOut, TWin> : ISqlWindowAble<TIn, TOut, TWin> { }

    /// <summary>
    /// DISTINCT o DISTINCT ON, va después del FROM list
    /// </summary>
    public interface ISqlDistinctAble<TIn, TOut, TWin> : ISqlWindowAble<TIn, TOut, TWin> { }

    /// <summary>
    /// The first JOIN after the initial FROM statement
    /// </summary>
    public interface ISqlFirstJoinAble<TIn, TOut, TWin> : ISqlDistinctAble<TIn, TOut, TWin>, IFirstJoinAble<TOut> { }

    /// <summary>
    /// JOINS after the first join
    /// </summary>
    public interface ISqlNextJoinAble<TIn, TOut, TWin> : ISqlDistinctAble<TIn, TOut, TWin>, INextJoinAble<TOut> { }
}
