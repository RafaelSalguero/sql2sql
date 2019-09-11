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
        IReadOnlyList<LambdaExpression> IndexExpressions { get; }

        /// <summary>
        /// Condición de la expresión del indice al que hace referencia el <see cref="IndexExpressions"/>
        /// </summary>
        LambdaExpression Where { get; }

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
        /// Lambda de 2 argumentos que crea una instancia donde define las columnas a actualizar. No necesariamente tiene que ser del mismo tipo que el de la expresión Value o Query,
        ///  queda a responsabilidad del programador que los nombres de las columnas generadas en el Set tengan los mismos nombres que el de la tabla.
        ///  
        /// El 1er argumento del lambda es el EXCLUDED de posgres, hace referencia a la fila propuesta para la insersión.
        /// El 2do argumento del lambda es la tabla del insert, hace referencia a la fila original.
        /// </summary>
        LambdaExpression Set { get; }

        /// <summary>
        /// Lambda con los mismos argumentos que el <see cref="Set"/>
        /// Determina si se va a realizar el UPDATE.
        /// </summary>
        LambdaExpression Where { get; }
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
        public OnConflictClause(IReadOnlyList<LambdaExpression> indexExpressions, LambdaExpression where, IOnConflictDoUpdateClause doUpdate)
        {
            IndexExpressions = indexExpressions;
            Where = where;
            DoUpdate = doUpdate;
        }

        public IReadOnlyList<LambdaExpression> IndexExpressions { get; }
        public LambdaExpression Where { get; }
        public IOnConflictDoUpdateClause DoUpdate { get; }
    }

    class OnConflictDoUpdateClause : IOnConflictDoUpdateClause
    {
        public OnConflictDoUpdateClause(LambdaExpression set, LambdaExpression where)
        {
            Set = set;
            Where = where;
        }

        public LambdaExpression Set { get; }
        public LambdaExpression Where { get; }
    }
}
