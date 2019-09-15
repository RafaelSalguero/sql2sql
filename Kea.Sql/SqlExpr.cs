using Sql2Sql.ExprTree;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Sql2Sql
{
    /// <summary>
    /// Expresiones utiles para el SQL
    /// </summary>
    public static class SqlExpr
    {
        /// <summary>
        /// (pred, cond) => bool
        /// Si pred es true devuelve cond, si pred es false devuelve true
        /// </summary>
        public static readonly Expression<Func<bool, bool, bool>> IfCond = (pred, cond) => !pred || cond;

        /// <summary>
        /// (str, patt) => bool
        /// Si pattern es null, devuelve true
        /// Devuelve true si str contiene a pattern, sin importar mayúsculas y minúsculas.
        /// </summary>
        public static readonly Expression<Func<string, string, bool>> ContainsStr = (str, patt) =>
            (patt == null) || (str.ToLower().Contains(patt.ToLower()));

        /// <summary>
        /// (val, patt) => bool
        /// Si patt == null, devuelve true.
        /// Devuelve true si val == patt
        /// </summary>
        public static readonly Expression<Func<object, object, bool>> EqualsNullable = (val, patt) =>
            (patt == null) || (val == patt);

        /// <summary>
        /// (min, max, val) => bool
        /// Crea una expresión que devuelve true si val se encuentra dentro del rango, se aceptan valores nulos para min y max.
        /// </summary>
        /// <typeparam name="T">Tipo que debe de soportar los operadores de comparasión</typeparam>
        public static Expression<Func<T?, T?, T?, bool>> Range<T>()
            where T : struct
        {
            Expression<Func<int?, int?, int?, bool>> expr = (min, max, v) =>
          (min == null || (v >= min)) &&
          (max == null || (v <= max));

            var subs = (Expression<Func<T?, T?, T?, bool>>)ReplaceVisitor.Replace(expr, new Dictionary<Expression, Expression>(), new Dictionary<Type, Type>
            {
                { typeof(int), typeof(T) }
            }, x => false);

            return subs;
        }
    }
}
