using Sql2Sql.Fluent.Data;
using Sql2Sql.SqlText.Insert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Fluent
{
    /// <summary>
    /// Una sentencia de INSERT
    /// </summary>
    public interface ISqlInsert : ISqlStatement { }

    /// <summary>
    /// Un INSERT que no genera resultados
    /// </summary>
    public interface ISqlInsertHasClause : ISqlInsert
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
    /// Un INSERT incompleto
    /// </summary>
    public interface ISqlInsertHasClauseIncomplete<TTable, TCols> : ISqlInsertIncomplete { }

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
    public interface IInsertConflictDoUpdate<TTable, TCols> : ISqlInsertHasClauseIncomplete<TTable, TCols> { }

    /// <summary>
    /// DO NOTHING del ON CONFLICT
    /// </summary>
    public interface IInsertConflictDoNothing<TTable, TCols> : ISqlInsertHasClauseIncomplete<TTable, TCols> { }

    /// <summary>
    /// DO NOTHING / DO UPDATE del ON CONFLICT
    /// </summary>
    public interface IInsertConflictDo<TTable, TCols> : IInsertConflictDoNothing<TTable, TCols>, IInsertConflictDoUpdate<TTable, TCols> { }

    /// <summary>
    /// WHERE del ON CONFLICT
    /// </summary>
    public interface IInsertConflictWhere<TTable, TCols> : IInsertConflictDo<TTable, TCols> { }

    /// <summary>
    /// WHERE del ON CONFLICT, para cuando no hay ninguna expresión de indices
    /// </summary>
    public interface IInsertConflictWhereEmptyIndex<TTable, TCols> : IInsertConflictDoNothing<TTable, TCols> { }

    /// <summary>
    /// Expresiones posteriores del indica del ON CONFLICT
    /// </summary>
    public interface IInsertConflictIndexExprThenBy<TTable, TCols> : IInsertConflictWhere<TTable, TCols> { }

    /// <summary>
    /// Expresiones posteriores del indica del ON CONFLICT, para cuando no hay ninguna expresión de indices
    /// </summary>
    public interface IInsertConflictEmptyIndexExprThenBy<TTable, TCols> : IInsertConflictWhereEmptyIndex<TTable, TCols> { }

}
