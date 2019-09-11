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
    public interface IInsertClause
    {
        /// <summary>
        /// Nombre de la tabla a la que se hará el insert
        /// </summary>
        string Table { get; }

        /// <summary>
        /// En caso de que se vaya a insertar un solo valor, la expresión devuelve el valor a insertar.
        /// Si la expresión devuelve una constante los valores se leen tal cual y se pegan como constantes en el INSERT.
        /// Si la expresión devuelve una inicialización, las expresiones de inicialización se pegan en el INSERT.
        /// Si <see cref="Query"/> no es null, este debe de ser null ya que solo se permite uno de los dos.
        /// 
        /// Note que este es el cuerpo de la expresión y no el <see cref="LambdaExpression"/>
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
        OnConflictClause OnConflict { get; }

        /// <summary>
        /// Expresión con un argumento que representa el cuerpo del RETURNING. Es similar a la expresión de un SELECT
        /// El argumento hace referencia a las filas insertadas.
        /// El retorno de la expresión es el resultado del RETURNING.
        /// El tipo del argumento es libre por lo que puede ser que los nombres de las columnas del returning no encajen con las de la tabla,
        /// por eso, normalmente el tipo del argumento será igual al tipo de la tabla.
        /// 
        /// Si es null indica que no hay cláusula RETURNING
        /// </summary>
        LambdaExpression Returning { get; }
    }

    public class InsertClause : IInsertClause
    {
        public InsertClause(string table, Expression value, ISelectClause query, OnConflictClause onConflict, LambdaExpression returning)
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
        public OnConflictClause OnConflict { get; }
        public LambdaExpression Returning { get; }

        public static InsertClause Empty => new InsertClause(null, null, null, null, null);
        public InsertClause SetTable(string table) => new InsertClause(table, Value, Query, OnConflict, Returning);
        public InsertClause SetValue(Expression value) => new InsertClause(Table, value, Query, OnConflict, Returning);
        public InsertClause SetQuery(ISelectClause query) => new InsertClause(Table, Value, query, OnConflict, Returning);
        public InsertClause SetOnConflict(OnConflictClause onConflict) => new InsertClause(Table, Value, Query, onConflict, Returning);
        public InsertClause SetReturning(LambdaExpression returning) => new InsertClause(Table, Value, Query, OnConflict, returning);
    }

    /// <summary>
    /// Una clausula del ON CONFLICT de un INSERT
    /// </summary>
    public class OnConflictClause
    {
        public OnConflictClause(IReadOnlyList<LambdaExpression> indexExpressions, LambdaExpression where, OnConflictDoUpdateClause doUpdate)
        {
            IndexExpressions = indexExpressions;
            Where = where;
            DoUpdate = doUpdate;
        }

        /// <summary>
        /// Las columnas o expresiones de indice que se van a revisar en el ON CONFLICT
        /// </summary>
        public IReadOnlyList<LambdaExpression> IndexExpressions { get; }

        /// <summary>
        /// Condición de la expresión del indice al que hace referencia el <see cref="IndexExpressions"/>
        /// </summary>
        public LambdaExpression Where { get; }


        /// <summary>
        /// En caso de que la acción sea DO UPDATE, es la cláusula.
        /// En caso de que la acción sea DO NOTHING, es null.
        /// </summary>
        public OnConflictDoUpdateClause DoUpdate { get; }

        public static OnConflictClause Empty => new OnConflictClause(new LambdaExpression[0], null, null);
        public OnConflictClause AddIndexExpr(LambdaExpression expr) => new OnConflictClause(IndexExpressions.Concat(new[] { expr }).ToList(), Where, DoUpdate);
        public OnConflictClause SetWhere(LambdaExpression where) => new OnConflictClause(IndexExpressions, where, DoUpdate);
        public OnConflictClause SetDoUpdate(OnConflictDoUpdateClause clause) => new OnConflictClause(IndexExpressions, Where, clause);
    }

    /// <summary>
    /// Clausula DO UPDATE de un ON CONFLICT
    /// </summary>
    public class OnConflictDoUpdateClause
    {
        public OnConflictDoUpdateClause(LambdaExpression set, LambdaExpression where)
        {
            Set = set;
            Where = where;
        }

        /// <summary>
        /// Lambda de 2 argumentos que crea una instancia donde define las columnas a actualizar. No necesariamente tiene que ser del mismo tipo que el de la expresión Value o Query,
        ///  queda a responsabilidad del programador que los nombres de las columnas generadas en el Set tengan los mismos nombres que el de la tabla.
        ///  
        /// El 1er argumento del lambda es el EXCLUDED de posgres, hace referencia a la fila propuesta para la insersión.
        /// El 2do argumento del lambda es la tabla del insert, hace referencia a la fila original.
        /// </summary>
        public LambdaExpression Set { get; }

        /// <summary>
        /// Lambda con los mismos argumentos que el <see cref="Set"/>
        /// Determina si se va a realizar el UPDATE.
        /// </summary>
        public LambdaExpression Where { get; }

        public static OnConflictDoUpdateClause Empty => new OnConflictDoUpdateClause(null, null);
        public OnConflictDoUpdateClause SetSet(LambdaExpression set) => new OnConflictDoUpdateClause(set, Where);
        public OnConflictDoUpdateClause SetWhere(LambdaExpression where) => new OnConflictDoUpdateClause(Set, where);
    }

    /// <summary>
    /// Todas las interfaces del INSERT
    /// </summary>
    interface ISqlInsertBuilder<TTable, TCols> :
        ISqlInsertValuesQueryAble<TTable, TCols>,
        IInsertConflictIndexExprThenBy<TTable, TCols>,
        IInsertConflictUpdateWhere<TTable, TCols>,
        IInsertConflictEmptyIndexExprThenBy<TTable, TCols>
    { }

    /// <summary>
    /// Builder de los inserts
    /// </summary>
    class InsertBuilder<TTable, TCols, TRet> :
        ISqlInsertBuilder<TTable, TCols>,
        ISqlInsertReturning<TRet>
    {
        public InsertBuilder(InsertClause clause)
        {
            Clause = clause;
        }

        public InsertClause Clause { get; }
    }
}
