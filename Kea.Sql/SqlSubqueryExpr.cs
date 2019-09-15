using Sql2Sql.ExprRewrite;
using Sql2Sql.Fluent;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sql2Sql
{
    /// <summary>
    /// Funciones de SQL
    /// </summary>
    public static partial class Sql
    {
        /// <summary>
        /// El operador EXISTS. Devuelve true si el subquery devuelve 1 o mas filas
        /// </summary>
        [AlwaysThrows]
        public static bool Exists(ISqlSelect subquery) => throw new SqlFunctionException();

        /// <summary>
        /// El operador IN. Devuelve true si <paramref name="expression"/> se encuentra por lo menos 1 vez en <paramref name="subquery"/>
        /// </summary>
        [AlwaysThrows]
        public static bool In<T>(T expression, ISqlSelect<T> subquery) => throw new SqlFunctionException();
    }
}
