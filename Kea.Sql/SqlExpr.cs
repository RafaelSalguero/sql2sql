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
        /// Si pattern es null, devuelve true
        /// Devuelve true si str contiene a pattern, sin importar mayúsculas y minúsculas.
        /// </summary>
        public static readonly Expression<Func<string, string, bool>> containsStr = (str, patt) =>
            (patt == null) || (str.ToLower().Contains(patt.ToLower()));

        /// <summary>
        /// (val, patt)
        /// Si patt == null, devuelve true.
        /// Devuelve true si val == patt
        /// </summary>
        public static readonly Expression<Func<int?, int?, bool>> equalsNullableInt = (val, patt) =>
            (patt == null) || (val == patt);
        
    }
}
