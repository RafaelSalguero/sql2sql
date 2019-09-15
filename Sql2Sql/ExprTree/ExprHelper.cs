using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.ExprTree
{
    public static class ExprHelper
    {
        /// <summary>
        /// Toma una expresión en la forma (Arg1) => Ret, y devuelve otra en la forma (Arg1, Arg2) => Ret, donde Arg2 es ignorado
        /// </summary>
        public static Expression<Func<T1, TRet>> AddParam<T1, TRet>(Expression<Func<TRet>> expr)
        {
            var arg1 = Expression.Parameter(typeof(T1));
            return Expression.Lambda<Func<T1, TRet>>(expr.Body, arg1);
        }

        /// <summary>
        /// Toma una expresión en la forma (Arg1) => Ret, y devuelve otra en la forma (Arg1, Arg2) => Ret, donde Arg2 es ignorado
        /// </summary>
        public static Expression<Func<T1, T2, TRet>> AddParam<T1, T2, TRet>(Expression<Func<T1, TRet>> expr)
        {
            var arg2 = Expression.Parameter(typeof(T2));
            return Expression.Lambda<Func<T1, T2, TRet>>(expr.Body, expr.Parameters[0], arg2);
        }
    }
}
