﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Sql2Sql.Fluent;
using Sql2Sql.Fluent.Data;
using Sql2Sql.SqlText;
using static Sql2Sql.ExprTree.ExprReplace;

namespace Sql2Sql
{
    /// <summary>
    /// Extensiones de SQL para el SELECT
    /// </summary>
    public static class SqlSelectExtensions
    {
        /// <summary>
        /// Agrega un SELECT a la cláusula WITH
        /// </summary>
        public static SqlWithFromList<TWith, TOut> Query<TWith, TOut>(this ISqlWith<TWith> with, Expression<Func<TWith, ISqlSelect<TOut>>> select)
        {
            var clauseExpr = SqlWith.SubqueryRawSubs(select.Body, select.Parameters[0]);
            var clause = (ISqlSelect<TOut>)SqlWith.GetSelectFromExpr(clauseExpr);

            var wi = new WithSelectClause(select.Parameters[0], with);

            return new SqlWithFromList<TWith, TOut>(wi, clause);
        }

        /// <summary>
        /// Indica que un subquery es escalar, por lo que se puede usar dentro de expresiones de Select
        /// </summary>
        public static T Scalar<T>(this ISqlSelect<T> subquery) =>
            throw new SqlFunctionException();

        /// <summary>
        /// Obtiene el SQL y los parámetros de un select, los parámetros se sustituyen para queries de Entity Framework
        /// </summary>
        public static SqlResult ToSql(this ISqlStatement statement) => statement.ToSql(ParamMode.EntityFramework);

        /// <summary>
        /// Obtiene el SQL y los parámetros de un select
        /// </summary>
        public static SqlResult ToSql(this ISqlStatement statement, ParamMode mode)
        {
            var dic = new SqlParamDic();
            var sql = StatementStr.StatementToString(statement, mode, dic).Sql;
            var pars = dic.Items.Select(x => new SqlParam(x.ParamName, x.GetValue(), x.GetParamType()));

            return new SqlResult(sql, pars.ToList());
        }

        //Joins:

        static JoinItems<T1, object> InternalJoinType<T1>(this ISqlSelectHasClause<T1, T1, object> left, JoinType type) =>
        new JoinItems<T1, object>(type, false, left, null);

        internal static IFirstJoinLateralAble<T1> InternalInner<T1>(this ISqlFirstJoinAble<T1, T1, object> left) => left.InternalJoinType(JoinType.Inner);

        /// <summary>
        /// An INNER JOIN
        /// </summary>
        [Obsolete("INNER is implicity, use just JOIN")]
        public static IFirstJoinLateralAble<T1> Inner<T1>(this ISqlFirstJoinAble<T1, T1, object> left) => left.InternalInner();

        /// <summary>
        /// An INNER JOIN
        /// </summary>
        [Obsolete("INNER is implicity, use just JOIN")]
        public static INextJoinLateralAble<T1> Inner<T1>(this ISqlNextJoinAble<T1, T1, object> left) => left.InternalJoinType(JoinType.Inner);

        /// <summary>
        /// A LEFT JOIN
        /// </summary>
        public static IFirstJoinLateralAble<T1> Left<T1>(this ISqlFirstJoinAble<T1, T1, object> left) => left.InternalJoinType(JoinType.Left);

        /// <summary>
        /// A LEFT JOIN
        /// </summary>
        public static INextJoinLateralAble<T1> Left<T1>(this ISqlNextJoinAble<T1, T1, object> left) => left.InternalJoinType(JoinType.Left);

        /// <summary>
        /// Inicia un RIGHT JOIN
        /// </summary>
        public static IFirstJoinLateralAble<T1> Right<T1>(this ISqlFirstJoinAble<T1, T1, object> left) =>
            new JoinItems<T1, object>(JoinType.Left, false, left, null);

        /// <summary>
        /// Inicia un CROSS JOIN
        /// </summary>
        public static IFirstJoinLateralAble<T1> Cross<T1>(this ISqlFirstJoinAble<T1, T1, object> left) =>
            new JoinItems<T1, object>(JoinType.Cross, false, left, null);

        /// <summary>
        /// Inicia un OUTTER JOIN
        /// </summary>
        public static IFirstJoinLateralAble<T1> Outter<T1>(this ISqlFirstJoinAble<T1, T1, object> left) =>
            new JoinItems<T1, object>(JoinType.Cross, false, left, null);

        /// <summary>
        /// Aplica un JOIN que no es LATERAL
        /// </summary>
        [Obsolete("Use Join<Table>()")]
        public static IFirstJoinOnAble<TL, TR> Join<TL, TR>(this IFirstJoinLateralAble<TL> left, SqlTable<TR> right) =>
            left.InternalJoin(right);

        /// <summary>
        /// Aplica un JOIN que no es LATERAL
        /// </summary>
        public static IFirstJoinOnAble<TL, TR> Join<TL, TR>(this IFirstJoinLateralAble<TL> left, IFromListItemTarget<TR> right) =>
            left.InternalJoin(right);

        /// <summary>
        /// Aplica un JOIN que no es LATERAL
        /// </summary>
        public static INextJoinOnAble<TL, TR> Join<TL, TR>(this INextJoinLateralAble<TL> left, IFromListItemTarget<TR> right) =>
            left.InternalJoin(right);

        internal static JoinItems<TL, TR> InternalJoin<TL, TR>(this IBaseLeftJoinAble<TL> left, IFromListItemTarget<TR> right)
        {
            var dummyP0 = Expression.Parameter(typeof(TL));
            var r = Expression.Lambda<Func<TL, IFromListItemTarget<TR>>>(Expression.Constant(right), dummyP0);

            return new JoinItems<TL, TR>(left.Type, false, left.Left, r);
        }

        /// <summary>
        /// A JOIN LATERAL
        /// </summary>
        public static IFirstJoinOnAble<TL, TR> Lateral<TL, TR>(this ISqlFirstJoinAble<TL, TL, object> left, Expression<Func<TL, IFromListItemTarget<TR>>> right) =>
            left.InternalInner().Lateral(right)
          ;

        /// <summary>
        /// A JOIN LATERAL
        /// </summary>
        public static INextJoinOnAble<TL, TR> Lateral<TL, TR>(this INextJoinLateralAble<TL> left, Expression<Func<TL, IFromListItemTarget<TR>>> right) =>
          new JoinItems<TL, TR>(left.Type, true, left.Left, right);

        /// <summary>
        /// A JOIN LATERAL
        /// </summary>
        public static IFirstJoinOnAble<TL, TR> Lateral<TL, TR>(this IFirstJoinLateralAble<TL> left, Expression<Func<TL, IFromListItemTarget<TR>>> right) =>
          new JoinItems<TL, TR>(left.Type, true, left.Left, right);

        #region Joins Ons

        static SqlSelectBuilder<TRet, TRet, object> InternalOnMap<T1, T2, TRet>(this IBaseLeftRightJoinOnAble<T1, T2> items, Expression<Func<T1, T2, TRet>> map, Expression<Func<TRet, bool>> on)
        {
            var it = new SqlJoin(items.Left.Clause.From, items.Right, map, on, items.Type, items.Lateral);
            return new SqlSelectBuilder<TRet, TRet, object>(SelectClause.InitFromItem<TRet>(it));
        }

        /// <summary>
        /// Indica la condición del JOIN, mapeando la parte izquierda y derecha a una tupla
        /// </summary>
        public static ISqlNextJoinAble<Tuple<T1, T2>, Tuple<T1, T2>, object> On<T1, T2>(this IFirstJoinOnAble<T1, T2> items, Expression<Func<Tuple<T1, T2>, bool>> on) =>
             items.InternalOnMap((a, b) => new Tuple<T1, T2>(a, b), on);

        /// <summary>
        /// Indica la condición del JOIN, mapeando las partes izquierdas y la parte derecha a una tupla
        /// </summary>
        public static ISqlNextJoinAble<Tuple<T1, T2>, Tuple<T1, T2>, object> On<T1, T2>(this INextJoinOnAble<Tuple<T1>, T2> items, Expression<Func<Tuple<T1, T2>, bool>> on) =>
            items.InternalOnMap((a, b) => new Tuple<T1, T2>(a.Item1, b), on);

        /// <summary>
        /// Indica la condición del JOIN, mapeando las partes izquierdas y la parte derecha a una tupla
        /// </summary>
        public static ISqlNextJoinAble<Tuple<T1, T2, T3>, Tuple<T1, T2, T3>, object> On<T1, T2, T3>(this INextJoinOnAble<Tuple<T1, T2>, T3> items, Expression<Func<Tuple<T1, T2, T3>, bool>> on) =>
            items.InternalOnMap((a, b) => new Tuple<T1, T2, T3>(a.Item1, a.Item2, b), on);

        /// <summary>
        /// Indica la condición del JOIN, mapeando las partes izquierdas y la parte derecha a una tupla
        /// </summary>
        public static ISqlNextJoinAble<Tuple<T1, T2, T3, T4>, Tuple<T1, T2, T3, T4>, object> On<T1, T2, T3, T4>(this INextJoinOnAble<Tuple<T1, T2, T3>, T4> items, Expression<Func<Tuple<T1, T2, T3, T4>, bool>> on) =>
            items.InternalOnMap((a, b) => new Tuple<T1, T2, T3, T4>(a.Item1, a.Item2, a.Item3, b), on);

        /// <summary>
        /// Indica la condición del JOIN, mapeando las partes izquierdas y la parte derecha a una tupla
        /// </summary>
        public static ISqlNextJoinAble<Tuple<T1, T2, T3, T4, T5>, Tuple<T1, T2, T3, T4, T5>, object> On<T1, T2, T3, T4, T5>(this INextJoinOnAble<Tuple<T1, T2, T3, T4>, T5> items, Expression<Func<Tuple<T1, T2, T3, T4, T5>, bool>> on) =>
            items.InternalOnMap((a, b) => new Tuple<T1, T2, T3, T4, T5>(a.Item1, a.Item2, a.Item3, a.Item4, b), on);

        /// <summary>
        /// Indica la condición del JOIN, mapeando las partes izquierdas y la parte derecha a una tupla
        /// </summary>
        public static ISqlNextJoinAble<Tuple<T1, T2, T3, T4, T5, T6>, Tuple<T1, T2, T3, T4, T5, T6>, object> On<T1, T2, T3, T4, T5, T6>(this INextJoinOnAble<Tuple<T1, T2, T3, T4, T5>, T6> items, Expression<Func<Tuple<T1, T2, T3, T4, T5, T6>, bool>> on) =>
            items.InternalOnMap((a, b) => new Tuple<T1, T2, T3, T4, T5, T6>(a.Item1, a.Item2, a.Item3, a.Item4, a.Item5, b), on);

        /// <summary>
        /// Indica la condición del JOIN, mapeando las partes izquierdas y la parte derecha a una tupla
        /// </summary>
        public static ISqlNextJoinAble<Tuple<T1, T2, T3, T4, T5, T6, T7>, Tuple<T1, T2, T3, T4, T5, T6, T7>, object> On<T1, T2, T3, T4, T5, T6, T7>(this INextJoinOnAble<Tuple<T1, T2, T3, T4, T5, T6>, T7> items, Expression<Func<Tuple<T1, T2, T3, T4, T5, T6, T7>, bool>> on) =>
            items.InternalOnMap((a, b) => new Tuple<T1, T2, T3, T4, T5, T6, T7>(a.Item1, a.Item2, a.Item3, a.Item4, a.Item5, a.Item6, b), on);

        /// <summary>
        /// Indica la condición del JOIN, mapeando las partes izquierdas y la parte derecha a una tupla
        /// </summary>
        public static ISqlNextJoinAble<JTuple<T1, T2, T3, T4, T5, T6, T7, T8>, JTuple<T1, T2, T3, T4, T5, T6, T7, T8>, object> On<T1, T2, T3, T4, T5, T6, T7, T8>(this INextJoinOnAble<Tuple<T1, T2, T3, T4, T5, T6, T7>, T8> items, Expression<Func<JTuple<T1, T2, T3, T4, T5, T6, T7, T8>, bool>> on) =>
            items.InternalOnMap((a, b) => new JTuple<T1, T2, T3, T4, T5, T6, T7, T8>(a.Item1, a.Item2, a.Item3, a.Item4, a.Item5, a.Item6, a.Item7, b), on);


        /// <summary>
        /// Indica la condición del JOIN, mapeando las partes izquierdas y la parte derecha a una tupla
        /// </summary>
        public static ISqlNextJoinAble<JTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>, JTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>, object> On<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this INextJoinOnAble<JTuple<T1, T2, T3, T4, T5, T6, T7, T8>, T9> items, Expression<Func<JTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>, bool>> on) =>
            items.InternalOnMap((a, b) => new JTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>(a.Item1, a.Item2, a.Item3, a.Item4, a.Item5, a.Item6, a.Item7, a.Item8, b), on);


        /// <summary>
        /// Indica la condición del JOIN, mapeando las partes izquierdas y la parte derecha a una tupla
        /// </summary>
        public static ISqlNextJoinAble<JTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, JTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, object> On<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this INextJoinOnAble<JTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>, T10> items, Expression<Func<JTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, bool>> on) =>
            items.InternalOnMap((a, b) => new JTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(a.Item1, a.Item2, a.Item3, a.Item4, a.Item5, a.Item6, a.Item7, a.Item8, a.Item9, b), on);


        /// <summary>
        /// Renombra los elementos de un JOIN, esto para que sea más claro su uso en el SELECT
        /// </summary>
        public static ISqlNextJoinAble<TOut, TOut, object> Alias<TIn, TOut>(this ISqlNextJoinAble<TIn, TIn, object> from, Expression<Func<TIn, TOut>> map)
        {
            var it = new FromListAlias(from.Clause.From, map);
            return new SqlSelectBuilder<TOut, TOut, object>(SelectClause.InitFromItem<TOut>(it));
        }
        #endregion

        #region Union
        static ISqlUnionAble<TIn, TOut, TWin> InternalPostUnion<TIn, TOut, TWin>(this ISqlSelectHasClause<TIn, TOut, TWin> input, UnionType type, UnionUniqueness uniqueness, ISqlQuery query) =>
            new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.AddUnion(new UnionClause(UnionType.Union, uniqueness, query)));

        static ISqlUnionAble<TIn, TOut, TWin> InternalPostUnion<TIn, TOut, TWin>(this ISqlSelectHasClause<TIn, TOut, TWin> input, UnionType type, ISqlQuery query) => input.InternalPostUnion(type, UnionUniqueness.Distinct, query);
        static ISqlUnionAble<TIn, TOut, TWin> InternalPostUnionAll<TIn, TOut, TWin>(this ISqlSelectHasClause<TIn, TOut, TWin> input, UnionType type, ISqlQuery query) => input.InternalPostUnion(type, UnionUniqueness.All, query);

        /// <summary>
        /// A UNION [DISTINCT]
        /// </summary>
        public static ISqlUnionAble<TIn, TOut, TWin> Union<TIn, TOut, TWin>(this ISqlUnionAble<TIn, TOut, TWin> input, ISqlSelect<TOut> query) => input.InternalPostUnion(UnionType.Union, query);

        /// <summary>
        /// A UNION ALL
        /// </summary>
        public static ISqlUnionAble<TIn, TOut, TWin> UnionAll<TIn, TOut, TWin>(this ISqlUnionAble<TIn, TOut, TWin> input, ISqlSelect<TOut> query) => input.InternalPostUnionAll(UnionType.Union, query);

        /// <summary>
        /// An INTERSECT [DISTINCT]
        /// </summary>
        public static ISqlUnionAble<TIn, TOut, TWin> Intersect<TIn, TOut, TWin>(this ISqlUnionAble<TIn, TOut, TWin> input, ISqlSelect<TOut> query) => input.InternalPostUnion(UnionType.Intersect, query);

        /// <summary>
        /// An INTERSECT ALL
        /// </summary>
        public static ISqlUnionAble<TIn, TOut, TWin> IntersectAll<TIn, TOut, TWin>(this ISqlUnionAble<TIn, TOut, TWin> input, ISqlSelect<TOut> query) => input.InternalPostUnionAll(UnionType.Intersect, query);

        /// <summary>
        /// An EXCEPT [DISTINCT]
        /// </summary>
        public static ISqlUnionAble<TIn, TOut, TWin> Except<TIn, TOut, TWin>(this ISqlUnionAble<TIn, TOut, TWin> input, ISqlSelect<TOut> query) => input.InternalPostUnion(UnionType.Except, query);

        /// <summary>
        /// An EXCEPT ALL
        /// </summary>
        public static ISqlUnionAble<TIn, TOut, TWin> ExceptAll<TIn, TOut, TWin>(this ISqlUnionAble<TIn, TOut, TWin> input, ISqlSelect<TOut> query) => input.InternalPostUnionAll(UnionType.Except, query);
        #endregion

        #region Select
        /// <summary>
        /// Inicia un SELECT DISTINCT
        /// </summary>
        public static ISqlWindowAble<T, T, object> Distinct<T>(this ISqlDistinctAble<T, T, object> input) => new SqlSelectBuilder<T, T, object>(input.Clause.SetDistinctType(SelectType.Distinct));

        /// <summary>
        /// Inicia un SELECT DISTINCT ON (expr1, ... exprN), para agregar mas expresiones utilice el .ThenBy
        /// </summary>
        public static ISqlDistinctOnThenByAble<T, T, object> DistinctOn<T>(this ISqlDistinctAble<T, T, object> input, Expression<Func<T, object>> expr) => new SqlSelectBuilder<T, T, object>(input.Clause.AddDistinctOn(expr));

        /// <summary>
        /// Agrega una expresión al DISTINCT ON
        /// </summary>
        public static ISqlDistinctOnThenByAble<T, T, object> ThenBy<T>(this ISqlDistinctOnThenByAble<T, T, object> input, Expression<Func<T, object>> expr) => new SqlSelectBuilder<T, T, object>(input.Clause.AddDistinctOn(expr));

        /// <summary>
        /// Indica la expresión del SELECT en función del FROM-list
        /// </summary>
        public static ISqlWherable<TIn, TOut, object> Select<TIn, TOut>(this ISqlSelectAble<TIn, TIn, object> input, Expression<Func<TIn, TOut>> select) =>
                new SqlSelectBuilder<TIn, TOut, object>(input.Clause.SetSelect(select));

        /// <summary>
        /// Indica la expresión del SELECT en función del FROM-list
        /// </summary>
        public static ISqlWherable<TIn, TOut, TWin> Select<TIn, TOut, TWin>(this ISqlSelectAble<TIn, TIn, TWin> input, Expression<Func<TIn, TOut>> select) =>
                new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.SetSelect(select));

        /// <summary>
        /// Indica la expresión del SELECT en función del FROM-list y de los WINDOW definidos
        /// </summary>
        public static ISqlWherable<TIn, TOut, TWin> Select<TIn, TOut, TWin>(this ISqlSelectAble<TIn, TIn, TWin> input, Expression<Func<TIn, TWin, TOut>> select) =>
                new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.SetSelect(select));

        /// <summary>
        /// Indica un GROUP BY (expr1, .... exprN), para agregar mas expresiones utilice el .ThenBy
        /// </summary>
        public static ISqlGroupByThenByAble<TIn, TOut, TWin> GroupBy<TIn, TOut, TWin>(this ISqlGroupByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr) =>
            new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.AddGroupBy(new GroupByExpr(expr)));

        /// <summary>
        /// Agrega una expresión al GROUP BY
        /// </summary>
        public static ISqlGroupByThenByAble<TIn, TOut, TWin> ThenBy<TIn, TOut, TWin>(this ISqlGroupByThenByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr) =>
            new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.AddGroupBy(new GroupByExpr(expr)));



        /// <summary>
        /// Indica un ORDER BY (expr1, ... exprN), para agregar mas expresiones utilice el .ThenBy
        /// </summary>
        public static ISqlOrderByThenByAble<TIn, TOut, TWin> OrderBy<TIn, TOut, TWin>(this ISqlOrderByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr, OrderByOrder order, OrderByNulls? nulls) =>
            new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.AddOrderBy(new OrderByExpr(expr, order, nulls)));

        /// <summary>
        /// Indica un ORDER BY (expr1, ... exprN), para agregar mas expresiones utilice el .ThenBy
        /// </summary>
        public static ISqlOrderByThenByAble<TIn, TOut, TWin> OrderBy<TIn, TOut, TWin>(this ISqlOrderByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr, OrderByOrder order) =>
            input.OrderBy(expr, order, null);

        /// <summary>
        /// Indica un ORDER BY (expr1, ... exprN), para agregar mas expresiones utilice el .ThenBy
        /// </summary>
        public static ISqlOrderByThenByAble<TIn, TOut, TWin> OrderBy<TIn, TOut, TWin>(this ISqlOrderByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr) =>
            input.OrderBy(expr, OrderByOrder.Asc);

        /// <summary>
        /// Agrega una expresión al ORDER BY
        /// </summary>
        public static ISqlOrderByThenByAble<TIn, TOut, TWin> ThenBy<TIn, TOut, TWin>(this ISqlOrderByThenByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr, OrderByOrder order, OrderByNulls? nulls) =>
            new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.AddOrderBy(new OrderByExpr(expr, order, nulls)));

        /// <summary>
        /// Agrega una expresión al ORDER BY
        /// </summary>
        public static ISqlOrderByThenByAble<TIn, TOut, TWin> ThenBy<TIn, TOut, TWin>(this ISqlOrderByThenByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr, OrderByOrder order) =>
            input.ThenBy(expr, order, null);

        /// <summary>
        /// Agrega una expresión al ORDER BY
        /// </summary>
        public static ISqlOrderByThenByAble<TIn, TOut, TWin> ThenBy<TIn, TOut, TWin>(this ISqlOrderByThenByAble<TIn, TOut, TWin> input, Expression<Func<TIn, object>> expr) =>
            input.ThenBy(expr, OrderByOrder.Asc);

        /// <summary>
        /// Indica un WHERE (expr)
        /// </summary>
        public static ISqlGroupByAble<TIn, TOut, TWin> Where<TIn, TOut, TWin>(this ISqlWherable<TIn, TOut, TWin> input, Expression<Func<TIn, bool>> where) =>
                new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.SetWhere(where));

        /// <summary>
        /// Indica un WHERE (expr) en función de los WINDOW definidos
        /// </summary>
        public static ISqlGroupByAble<TIn, TOut, TWin> Where<TIn, TOut, TWin>(this ISqlWherable<TIn, TOut, TWin> input, Expression<Func<TIn, TWin, bool>> where) =>
                new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.SetWhere(where));

        /// <summary>
        /// Indica un LIMIT
        /// </summary>
        public static ISqlUnionAble<TIn, TOut, TWin> Limit<TIn, TOut, TWin>(this ISqlLimitAble<TIn, TOut, TWin> input, int limit) =>
                new SqlSelectBuilder<TIn, TOut, TWin>(input.Clause.SetLimit(limit));
        #endregion

        #region Window
        /// <summary>
        /// Indica una definición de una o mas WINDOWs en forma de un objeto
        /// </summary>
        /// <param name="windows">Función que toma el creador de WINDOW como parametro y devuelve un objeto anónimo donde cada propiedad de este objeto es un WINDOW</param>
        public static ISqlWindowAble<TIn, TIn, TWinOut> Window<TIn, TWinOut>(this ISqlWindowAble<TIn, TIn, object> input, Func<ISqlWindowItemAble<TIn>, TWinOut> windows)
        {
            var builder = new SqlWindowBuilder<TIn>(null, null, new SqlWindowClause( null, null, null));
            var ws = new WindowClauses(windows(builder));
            return new SqlSelectBuilder<TIn, TIn, TWinOut>(input.Clause.SetWindow(ws));
        }

        /// <summary>
        /// Declare a collection of windows, where each window is a property of the result of <paramref name="windows"/>
        /// </summary>
        /// <param name="windows">Función que toma el creador de WINDOW como parametro y devuelve un objeto anónimo donde cada propiedad de este objeto es un WINDOW</param>
        public static ISqlWindowAble<TIn, TIn, TWinOut> Window<TIn, TWinIn, TWinOut>(this ISqlWindowAble<TIn, TIn, TWinIn> input, Func<ISqlWindowItemAble<TIn>, TWinIn, TWinOut> windows)
        {
            var builder = new SqlWindowBuilder<TIn>(input.Clause.Window, null, new SqlWindowClause( null, null, null));
            var winInput = (TWinIn)input.Clause.Window.Windows;
            var ws = new WindowClauses(windows(builder, winInput));
            return new SqlSelectBuilder<TIn, TIn, TWinOut>(input.Clause.SetWindow(ws));
        }

        public static ISqlWindowPartitionByThenByAble<TIn> PartitionBy<TIn>(this ISqlWindowPartitionByAble<TIn> input, Expression<Func<TIn, object>> expr)
        {
            var old = new List<PartitionByExpr>();
            old.Add(new PartitionByExpr(expr));
            return new SqlWindowBuilder<TIn>(input.Input, input, input.Current.SetPartitionBy(old));
        }
        public static ISqlWindowPartitionByThenByAble<TIn> ThenBy<TIn>(this ISqlWindowPartitionByThenByAble<TIn> input, Expression<Func<TIn, object>> expr)
        {
            var old = input.Current.PartitionBy.ToList();
            old.Add(new PartitionByExpr(expr));
            return new SqlWindowBuilder<TIn>(input.Input, input, input.Current.SetPartitionBy(old));
        }
        public static ISqlWindowOrderByThenByAble<TIn> OrderBy<TIn>(this ISqlWindowOrderByAble<TIn> input, Expression<Func<TIn, object>> expr, OrderByOrder order = OrderByOrder.Asc, OrderByNulls? nulls = null)
        {
            var old = new List<OrderByExpr>();
            old.Add(new OrderByExpr(expr, order, nulls));
            return new SqlWindowBuilder<TIn>(input.Input, input, input.Current.SetOrderBy(old));
        }
        public static ISqlWindowOrderByThenByAble<TIn> ThenBy<TIn, TOut>(this ISqlWindowOrderByThenByAble<TIn> input, Expression<Func<TIn, object>> expr, OrderByOrder order = OrderByOrder.Asc, OrderByNulls? nulls = null)
        {
            var old = input.Current.OrderBy.ToList();
            old.Add(new OrderByExpr(expr, order, nulls));
            return new SqlWindowBuilder<TIn>(input.Input, input, input.Current.SetOrderBy(old));
        }
        static ISqlWindowFrameStartBetweenAble<TIn> FrameGrouping<TIn>(this ISqlWindowFrameAble<TIn> input, WinFrameGrouping grouping)
        {
            var newFrame = new SqlWinFrame(grouping, null, null, null);
            return new SqlWindowBuilder<TIn>(input.Input, input, input.Current.SetFrame(newFrame));
        }

        public static ISqlWindowFrameStartBetweenAble<TIn> Range<TIn, TOut>(this ISqlWindowFrameAble<TIn> input) =>
                input.FrameGrouping(WinFrameGrouping.Range);

        public static ISqlWindowFrameStartBetweenAble<TIn> Rows<TIn>(this ISqlWindowFrameAble<TIn> input) =>
                input.FrameGrouping(WinFrameGrouping.Rows);

        public static ISqlWindowFrameStartBetweenAble<TIn> Groups<TIn, TOut>(this ISqlWindowFrameAble<TIn> input) =>
                input.FrameGrouping(WinFrameGrouping.Groups);

        static ISqlWindowFrameEndExclusionAble<TIn> Start<TIn>(this ISqlWindowFrameStartAble<TIn> input, WinFrameStartEnd startEnd, int? offset = null)
        {
            var old = input.Current.Frame;
            return new SqlWindowBuilder<TIn>(input.Input, input.Previous, input.Current.SetFrame(old.SetStart(new SqlWindowFrameStartEnd(startEnd, offset))));
        }

        static ISqlWindowFrameExclusionAble<TIn> End<TIn>(this ISqlWindowFrameEndAble<TIn> input, WinFrameStartEnd startEnd, int? offset = null)
        {
            var old = input.Current.Frame;
            return new SqlWindowBuilder<TIn>(input.Input, input.Previous, input.Current.SetFrame(old.SetEnd(new SqlWindowFrameStartEnd(startEnd, offset))));
        }

        //START:

        public static ISqlWindowFrameEndExclusionAble<TIn> UnboundedPreceding<TIn>(this ISqlWindowFrameStartAble<TIn> input) =>
                input.Start(WinFrameStartEnd.UnboundedPreceding);

        public static ISqlWindowFrameEndExclusionAble<TIn> Preceding<TIn>(this ISqlWindowFrameStartAble<TIn> input, int offset) =>
                input.Start(WinFrameStartEnd.OffsetPreceding, offset);

        public static ISqlWindowFrameEndExclusionAble<TIn> CurrentRow<TIn>(this ISqlWindowFrameStartAble<TIn> input) =>
                input.Start(WinFrameStartEnd.CurrentRow);

        public static ISqlWindowFrameEndExclusionAble<TIn> Following<TIn>(this ISqlWindowFrameStartAble<TIn> input, int offset) =>
                input.Start(WinFrameStartEnd.OffsetFollowing, offset);

        public static ISqlWindowFrameEndExclusionAble<TIn> UnboundedFollowing<TIn>(this ISqlWindowFrameStartAble<TIn> input) =>
                input.Start(WinFrameStartEnd.UnboundedFollowing);


        //END:
        public static ISqlWindowFrameExclusionAble<TIn> AndUnboundedPreceding<TIn>(this ISqlWindowFrameEndAble<TIn> input) =>
             input.End(WinFrameStartEnd.UnboundedFollowing);

        public static ISqlWindowFrameExclusionAble<TIn> AndPreceding<TIn>(this ISqlWindowFrameEndAble<TIn> input, int offset) =>
            input.End(WinFrameStartEnd.OffsetPreceding, offset);

        public static ISqlWindowFrameExclusionAble<TIn> AndCurrentRow<TIn>(this ISqlWindowFrameEndAble<TIn> input) =>
            input.End(WinFrameStartEnd.CurrentRow);

        public static ISqlWindowFrameExclusionAble<TIn> AndFollowing<TIn>(this ISqlWindowFrameEndAble<TIn> input, int offset) =>
            input.End(WinFrameStartEnd.OffsetFollowing, offset);

        public static ISqlWindowFrameExclusionAble<TIn> AndUnboundedFollowing<TIn>(this ISqlWindowFrameEndAble<TIn> input) =>
            input.End(WinFrameStartEnd.UnboundedFollowing);

        //Exclusion:

        static ISqlWindowFrame<TIn> Exclusion<TIn>(this ISqlWindowFrameExclusionAble<TIn> input, WinFrameExclusion exclusion)
        {
            var old = input.Current.Frame;
            return new SqlWindowBuilder<TIn>(input.Input, input.Previous, input.Current.SetFrame(old.SetExclusion(exclusion)));
        }

        public static ISqlWindowFrame<TIn> ExcludeCurrentRow<TIn>(this ISqlWindowFrameExclusionAble<TIn> input) =>
            input.Exclusion(WinFrameExclusion.CurrentRow);

        public static ISqlWindowFrame<TIn> ExcludeGroup<TIn>(this ISqlWindowFrameExclusionAble<TIn> input) =>
            input.Exclusion(WinFrameExclusion.Group);

        public static ISqlWindowFrame<TIn> ExcludeTies<TIn>(this ISqlWindowFrameExclusionAble<TIn> input) =>
            input.Exclusion(WinFrameExclusion.Ties);

        public static ISqlWindowFrame<TIn> ExcludeNoOthers<TIn>(this ISqlWindowFrameExclusionAble<TIn> input) =>
            input.Exclusion(WinFrameExclusion.NoOthers);
        #endregion
    }
}
