using Sql2Sql.Fluent;
using Sql2Sql.Fluent.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.SqlText.Insert
{
    /// <summary>
    /// Data needed to generate an INSERT
    /// </summary>
    public class InsertClause 
    {
        public InsertClause(string table, Expression value, ISelectClause query, OnConflictClause onConflict, LambdaExpression returning)
        {
            Table = table;
            Value = value;
            Query = query;
            OnConflict = onConflict;
            Returning = returning;
        }

        /// <summary>
        /// The name of the table
        /// </summary>
        public string Table { get; }

        /// <summary>
        /// If this is a single row insert, this expression returns the row to insert
        /// The row to insert must be an initialization expression
        /// If <see cref="Query"/> is not null, this must be null
        /// 
        /// Note that this is the expression body and not a lambda expression
        /// </summary>
        public Expression Value { get; }

        /// <summary>
        /// If this is a query insert, is the query that returns the rows to insert.
        /// Note that this is a <see cref="ISelectClause"/> and not a <see cref="ISqlSelect"/> or a <see cref="IFromListItem"/> since this can't be a raw SQL query
        /// since the query object is used to get the insert columns
        /// </summary>
        public ISelectClause Query { get; }

        /// <summary>
        /// ON CONFLICT clause or null
        /// </summary>
        public OnConflictClause OnConflict { get; }

        /// <summary>
        /// Single argument lambda expression representing the body of the RETURNING clause. Similar to the SELECT expression.
        /// The argument references the inserted rows
        /// The type of the argument is not checked, but the fluent API uses always the same type as the table
        /// 
        /// If null the INSERT doesn't have a RETURNING clause
        /// </summary>
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

        public override string ToString()
        {
            return this.ToSql(SqlText.ParamMode.Substitute).Sql;
        }
    }
}
