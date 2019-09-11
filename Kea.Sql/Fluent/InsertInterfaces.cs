using KeaSql.Fluent.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Fluent
{


    /// <summary>
    /// Un INSERT que no genera resultados
    /// </summary>
    public interface ISqlInsertHasClause : ISqlStatement
    {
        /// <summary>
        /// Cláusula del INSERT
        /// </summary>
        InsertClause Clause { get; }
    }

    /// <summary>
    /// Un INSERT que genera resultados
    /// </summary>
    public interface ISqlInsertReturning<out T> : ISqlQuery<T>, ISqlInsertHasClause { }

    /// <summary>
    /// Indica un INSERT que esta incompleto en su definición
    /// </summary>
    public interface ISqlInsertIncomplete
    {
        /// <summary>
        /// Cláusula del INSERT
        /// </summary>
        InsertClause Clause { get; }
    }

    /// <summary>
    /// Un INSERT que no genera resultados, tipado con el tipo de la tabla
    /// </summary>
    public interface ISqlInsertHasClause<TTable, TCols> : ISqlInsertHasClause { }

    /// <summary>
    /// El RETURNING de un INSERT, va después del ON CONFLICT (opcional)
    /// </summary>
    public interface ISqlInsertReturningAble<TTable, TCols> : ISqlInsertHasClause<TTable, TCols> { }

    /// <summary>
    /// El ON CONFLICT del INSERT,  va despues de los VALUES o del query
    /// </summary>
    public interface ISqlInsertOnConflictAble<TTable, TCols> : ISqlInsertReturningAble<TTable, TCols> { }

    /// <summary>
    /// Un INSERT donde ya se definió la tabla y se pueden definir los VALUES o el query
    /// </summary>
    public interface ISqlInsertValuesQueryAble<TTable, TCols> : ISqlInsertOnConflictAble<TTable, TCols> { }

    /// <summary>
    /// WHERE del DO UPDATE del ON CONFLICT
    /// </summary>
    public interface IInsertConflictUpdateWhere<TTable, TCols> : ISqlInsertReturningAble<TTable, TCols> { }

    /// <summary>
    /// DO UPDATE del ON CONFLICT
    /// </summary>
    public interface IInsertConflictDoUpdate<TTable, TCols> : IInsertConflictUpdateWhere<TTable, TCols> { }

    /// <summary>
    /// DO NOTHING del ON CONFLICT
    /// </summary>
    public interface IInsertConflictDoNothing<TTable, TCols> : IInsertConflictUpdateWhere<TTable, TCols> { }

    /// <summary>
    /// DO NOTHING / DO UPDATE del ON CONFLICT
    /// </summary>
    public interface IInsertConflictDo<TTable, TCols> : IInsertConflictDoNothing<TTable, TCols>, IInsertConflictDoUpdate<TTable, TCols> { }

    /// <summary>
    /// WHERE del ON CONFLICT
    /// </summary>
    public interface IInsertConflictWhere<TTable, TCols> : IInsertConflictDo<TTable, TCols> { }

    /// <summary>
    /// Expresiones posteriores del indica del ON CONFLICT
    /// </summary>
    public interface IInsertConflictIndexExprThenBy<TTable, TCols> : IInsertConflictWhere<TTable, TCols> { }

}
