using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Fluent.Data
{
    /// <summary>
    /// Datos para generar un INSERT
    /// </summary>
    interface IInsertClause
    {
        /// <summary>
        /// Nombre de la tabla a la que se hará el insert
        /// </summary>
        string Table { get; }

        /// <summary>
        /// En caso de que se vaya a insertar un solo valor, la expresión devuelve el valor a insertar.
        /// Si la expresión devuelve una constante los valores se leen tal cual y se pegan como constantes en el INSERT.
        /// Si la expresión devuelve una inicialización, las expresiones de inicialización se pegan en el INSERT.
        /// Si <see cref="Query"/> no es null, este debe de ser null ya que solo se permite uno de los dos
        /// </summary>
        Expression Value { get; }

        /// <summary>
        /// En caso de que se vayan a insertar varios valores, es el query que devuelve los valores a insertar. Note que el tipo es un <see cref="ISelectClause"/> y no
        /// un <see cref="IFromListItem"/> o un <see cref="ISqlSelect"/> ya que este query no puede ser un RAW, ya que se ocupan saber los nombres de las columnas
        /// </summary>
        ISelectClause Query { get; }

        /// <summary>
        /// Cláusula de ON CONFLICT o null
        /// </summary>
        IOnConflictClause OnConflict { get; }

        /// <summary>
        /// Expresión del RETURNING o null
        /// </summary>
        Expression Returning { get; }
    }

    /// <summary>
    /// Una clausula del ON CONFLICT de un INSERT
    /// </summary>
    interface IOnConflictClause
    {
        /// <summary>
        /// Las columnas o expresiones de indice que se van a revisar en el ON CONFLICT
        /// </summary>
        IReadOnlyList<Expression> IndexExpressions { get; }

        /// <summary>
        /// Condición del indice, puede ser null
        /// </summary>
        Expression Where { get; }

        /// <summary>
        /// En caso de que la acción sea DO UPDATE, es la cláusula.
        /// En caso de que la acción sea DO NOTHING, es null.
        /// </summary>
        IOnConflictDoUpdateClause DoUpdate { get; }
    }

    /// <summary>
    /// Clausula DO UPDATE de un ON CONFLICT
    /// </summary>
    interface IOnConflictDoUpdateClause
    {
        /// <summary>
        /// Expresión que crea una instancia donde define las columnas a actualizar
        /// </summary>
        Expression Set { get; }

        /// <summary>
        /// Condicional en función de la fila a agregar
        /// </summary>
        Expression Where { get; }
    }

    class InsertClause : IInsertClause
    {
        public InsertClause(string table, Expression value, ISelectClause query, IOnConflictClause onConflict, Expression returning)
        {
            Table = table;
            Value = value;
            Query = query;
            OnConflict = onConflict;
            Returning = returning;
        }

        public string Table { get; }
        public Expression Value { get; }
        public ISelectClause Query { get; }
        public IOnConflictClause OnConflict { get; }
        public Expression Returning { get; }
    }

    class OnConflictClause : IOnConflictClause
    {
        public OnConflictClause(IReadOnlyList<Expression> indexExpressions, Expression where, IOnConflictDoUpdateClause doUpdate)
        {
            IndexExpressions = indexExpressions;
            Where = where;
            DoUpdate = doUpdate;
        }

        public IReadOnlyList<Expression> IndexExpressions { get; }
        public Expression Where { get; }
        public IOnConflictDoUpdateClause DoUpdate { get; }
    }

    class OnConflictDoUpdateClause : IOnConflictDoUpdateClause
    {
        public OnConflictDoUpdateClause(Expression set, Expression where)
        {
            Set = set;
            Where = where;
        }

        public Expression Set { get; }
        public Expression Where { get; }
    }
}
