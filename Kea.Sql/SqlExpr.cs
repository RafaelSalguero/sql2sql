using System;
using System.Linq.Expressions;

namespace KeaSql
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
        public static readonly Expression<Func<bool, bool, bool>> ifCond = (pred, cond) => !pred || cond;

        /// <summary>
        /// (str, patt) => bool
        /// Si pattern es null, devuelve true
        /// Devuelve true si str contiene a pattern, sin importar mayúsculas y minúsculas.
        /// </summary>
        public static readonly Expression<Func<string, string, bool>> containsStr = (str, patt) =>
            (patt == null) || (str.ToLower().Contains(patt.ToLower()));

        /// <summary>
        /// (val, patt) => bool
        /// Si patt == null, devuelve true.
        /// Devuelve true si val == patt
        /// </summary>
        public static readonly Expression<Func<int?, int?, bool>> equalsNullableInt = (val, patt) =>
            (patt == null) || (val == patt);

        /// <summary>
        /// (min, max, val) => bool
        /// Devuelve true si val se encuentra dentro del rango, se aceptan valores nulos para min y max.
        /// </summary>
        public static readonly Expression<Func<DateTimeOffset?, DateTimeOffset?, DateTimeOffset?, bool>> range = (min, max, v) =>
           (min == null || (v >= min)) &&
           (max == null || (v <= max));
    }
}
