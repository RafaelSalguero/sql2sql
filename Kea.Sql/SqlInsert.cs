using KeaSql.Fluent;
using KeaSql.Fluent.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql
{
    public static partial class Sql
    {
        /// <summary>
        /// Comienza un INSERT a una tabla
        /// </summary>
        public static ISqlInsertValuesQueryAble<TTable, object> InsertInto<TTable>(SqlTable<TTable> table) =>
            new InsertBuilder<TTable, object, object>(InsertClause.Empty.SetTable(table.Name));

        /// <summary>
        /// Comienza un INSERT a una tabla, el nombre de la tabla lo obtiene del nombre del tipo
        /// </summary>
        public static ISqlInsertValuesQueryAble<TTable, object> InsertInto<TTable>() => InsertInto(new SqlTable<TTable>());

        /// <summary>
        /// Establece las columnas y los VALUES de un INSERT
        /// </summary>
        public static ISqlInsertOnConflictAble<TTable, TCols> Values<TTable, TCols>(this ISqlInsertValuesQueryAble<TTable, object> x, Expression<Func<TCols>> values) =>
            new InsertBuilder<TTable, TCols, object>(x.Clause.SetValue(values.Body));

        /// <summary>
        /// Establece las columans y el query de un INSERT
        /// </summary>
        public static ISqlInsertOnConflictAble<TTable, TOut> Query<TTable, TIn, TOut, TWin>(this ISqlInsertValuesQueryAble<TTable, object> x, ISqlSelectHasClause<TIn, TOut, TWin> query) =>
            new InsertBuilder<TTable, TOut, object>(x.Clause.SetQuery(query.Clause));

        /// <summary>
        /// Establece el ON CONFLICT del INSERT
        /// </summary>
        public static IInsertConflictEmptyIndexExprThenBy<TTable, TCols> OnConflict<TTable, TCols>(this ISqlInsertOnConflictAble<TTable, TCols> x) =>
            new InsertBuilder<TTable, TCols, object>(x.Clause.SetOnConflict(OnConflictClause.Empty));

        /// <summary>
        /// Establece el ON CONFLICT del INSERT y la primera expresión del indice
        /// </summary>
        public static IInsertConflictIndexExprThenBy<TTable, TCols> OnConflict<TTable, TCols, TIndex>(this ISqlInsertOnConflictAble<TTable, TCols> x, Expression<Func<TTable, TIndex>> indexExpr) =>
            new InsertBuilder<TTable, TCols, object>(x.Clause.SetOnConflict(OnConflictClause.Empty.AddIndexExpr(indexExpr)));

        /// <summary>
        /// Establece una expresión posterior al indice del ON CONFLICT
        /// </summary>
        public static IInsertConflictIndexExprThenBy<TTable, TCols> ThenBy<TTable, TCols, TIndex>(this IInsertConflictIndexExprThenBy<TTable, TCols> x, Expression<Func<TTable, TIndex>> indexExpr) =>
            new InsertBuilder<TTable, TCols, object>(x.Clause.SetOnConflict(x.Clause.OnConflict.AddIndexExpr(indexExpr)));

        /// <summary>
        /// Establece el WHERE del ON CONFLICT
        /// </summary>
        /// <param name="indexExpr">Expresión condicional del índice</param>
        public static IInsertConflictDo<TTable, TCols> Where<TTable, TCols>(this IInsertConflictWhere<TTable, TCols> x, Expression<Func<TTable, bool>> indexExpr) =>
            new InsertBuilder<TTable, TCols, object>(x.Clause.SetOnConflict(
                x.Clause.OnConflict.SetWhere(indexExpr)
                ));

        /// <summary>
        /// DO NOTHING del ON CONFLICT
        /// </summary>
        public static ISqlInsertReturningAble<TTable, TCols> DoNothing<TTable, TCols>(this IInsertConflictDoNothing<TTable, TCols> x) =>
            new InsertBuilder<TTable, TCols, object>(x.Clause.SetOnConflict(x.Clause.OnConflict.SetDoUpdate(null)));

        /// <summary>
        /// DO UPDATE del ON CONFLICT
        /// </summary>
        /// <param name="setExpr">
        /// Representa al SET del DO UPDATE. 
        /// El primer argumento es el EXCLUDE, la fila propuesta para la insersión.
        /// El segundo argumento es la fila original.
        /// </param>
        public static IInsertConflictUpdateWhere<TTable, TCols> DoUpdate<TTable, TCols>(this IInsertConflictDoUpdate<TTable, TCols> x, Expression<Func<TTable, TTable, TTable>> setExpr) =>
            new InsertBuilder<TTable, TCols, object>(
                x.Clause.SetOnConflict(
                    x.Clause.OnConflict.SetDoUpdate(
                        OnConflictDoUpdateClause.Empty.SetSet(setExpr)
                )));

        /// <summary>
        /// DO UPDATE del ON CONFLICT
        /// </summary>
        /// <param name="setExpr">
        /// Representa al SET del DO UPDATE. 
        /// El primer argumento es el EXCLUDE, la fila propuesta para la insersión.
        /// </param>
        public static IInsertConflictUpdateWhere<TTable, TCols> DoUpdate<TTable, TCols>(this IInsertConflictDoUpdate<TTable, TCols> x, Expression<Func<TTable, TTable>> setExpr) =>
            x.DoUpdate(ExprTree.ExprHelper.AddParam<TTable, TTable, TTable>(setExpr));

        /// <summary>
        /// DO UPDATE del ON CONFLICT
        /// </summary>
        /// <param name="setExpr">
        /// Representa al SET del DO UPDATE. 
        /// </param>
        public static IInsertConflictUpdateWhere<TTable, TCols> DoUpdate<TTable, TCols>(this IInsertConflictDoUpdate<TTable, TCols> x, Expression<Func<TTable>> setExpr) =>
            x.DoUpdate(ExprTree.ExprHelper.AddParam<TTable, TTable>(setExpr));


        /// <summary>
        /// WHERE del DO UPDATE del ON CONFLICT
        /// </summary>
        /// <param name="where">
        /// Solamente las filas que devuelvan TRUE serán actualizadas
        /// El 1er argumento del lambda es el EXCLUDED de posgres, hace referencia a la fila propuesta para la insersión.
        /// El 2do argumento del lambda es la tabla del insert, hace referencia a la fila original.
        /// </param>
        public static ISqlInsertReturningAble<TTable, TCols> Where<TTable, TCols>(this IInsertConflictUpdateWhere<TTable, TCols> x, Expression<Func<TTable, TTable, bool>> where) =>
            new InsertBuilder<TTable, TCols, object>(
                x.Clause.SetOnConflict(
                    x.Clause.OnConflict.SetDoUpdate(
                        x.Clause.OnConflict.DoUpdate.SetWhere(where)
                        )
                    )
                );

        /// <summary>
        /// WHERE del DO UPDATE del ON CONFLICT
        /// </summary>
        /// <param name="where">
        /// Solamente las filas que devuelvan TRUE serán actualizadas
        /// El 1er argumento del lambda es el EXCLUDED de posgres, hace referencia a la fila propuesta para la insersión.
        /// </param>
        public static ISqlInsertReturningAble<TTable, TCols> Where<TTable, TCols>(this IInsertConflictUpdateWhere<TTable, TCols> x, Expression<Func<TTable, bool>> where) =>
            x.Where(ExprTree.ExprHelper.AddParam<TTable, TTable, bool>(where));

        /// <summary>
        /// RETURNING del INSERT
        /// </summary>
        /// <param name="returning">Expresión que devuelve las filas a devolver</param>
        public static ISqlInsertReturning<TRet> Returning<TTable, TCols, TRet>(this ISqlInsertReturningAble<TTable, TCols> x, Expression<Func<TTable, TRet>> returning) =>
            new InsertBuilder<TTable, TCols, TRet>(
                x.Clause.SetReturning(returning)
                );
    }
}