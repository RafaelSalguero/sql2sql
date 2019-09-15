using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Sql2Sql.ExprRewrite
{
    public static class ExprEval
    {
        public class EvalExprResult<T>
        {
            readonly T value;

            public EvalExprResult(T value, bool success, Exception exception)
            {
                this.value = value;
                Success = success;
                Exception = exception;
            }

            /// <summary>
            /// Si Success == true, devuelve el valor, si no, lanza una excepción
            /// </summary>
            public T Value
            {
                get
                {
                    if (Success) return value;
                    throw new ArgumentException($"No se pudo evaluar la expresión", Exception);
                }
            }
            public bool Success { get; }
            public Exception Exception { get; }
        }

        /// <summary>
        /// Trata de evaluar una expresión a su forma constante, si lo logra devuelve la expresión reducida, si no, devuelve la expresión original
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static Expression EvalExprExpr(Expression expr)
        {
            if (TryEvalExpr(expr, out object cons))
            {
                return Expression.Constant(cons);
            }
            return expr;
        }

        public static EvalExprResult<T> EvalExpr<T>(Expression expr)
        {
            var eval = EvalExprObj(expr);
            if (eval.Success)
            {
                return new EvalExprResult<T>((T)eval.Value, true, null);
            }
            return new EvalExprResult<T>(default(T), false, eval.Exception);

        }

        /// <summary>
        /// Devuelve null si no pudo evaluar la expresión
        /// </summary>
        /// <param name="bin"></param>
        /// <returns></returns>
        static EvalExprResult<object> EvalBinExpr(BinaryExpression bin)
        {
            var left = EvalExprObj(bin.Left);
            var right = EvalExprObj(bin.Right);

            if (!(left.Success && right.Success))
            {
                return new EvalExprResult<object>(null, false, null);
            }

            return null;
        }

        static EvalExprResult<object> EvalUnaryExpr(UnaryExpression expr)
        {
            var op = EvalExprObj(expr.Operand);
            if (!op.Success)
            {
                return new EvalExprResult<object>(null, false, null);
            }

            return null;
        }

        static EvalExprResult<object> EvalMemberExpr(MemberExpression mem)
        {
            object value = null;
            if (mem.Expression != null)
            {
                //Note que pueden haber miembros estáticos que no tengan Expressionk
                var expr = EvalExprObj(mem.Expression);
                if (!expr.Success)
                {
                    return new EvalExprResult<object>(null, false, null);
                }
                value = expr.Value;
            }

            if (mem.Member is PropertyInfo prop)
            {
                if (value == null && !prop.GetMethod.IsStatic)
                {
                    //No se puede acceder a una propiedad  de null
                    return new EvalExprResult<object>(null, false, null);
                }

                return new EvalExprResult<object>(prop.GetValue(value), true, null);
            }
            else if (mem.Member is FieldInfo field)
            {
                if (value == null && !field.IsStatic)
                {
                    //No se puede acceder a un field de null
                    return new EvalExprResult<object>(null, false, null);
                }

                return new EvalExprResult<object>(field.GetValue(value), true, null);
            }

            return null;
        }

        /// <summary>
        /// Evalua una expresión. Se trata de evitar las excepciones en el DynamicInvoke por cuestion de rendimiento
        /// </summary>
        static EvalExprResult<object> EvalExprObj(Expression expr)
        {
            if (expr is ConstantExpression exprCons)
            {
                return new EvalExprResult<object>(exprCons.Value, true, null);
            }
            else if (expr is ParameterExpression)
            {
                //Los parámetros no se pueden evaluar:
                return new EvalExprResult<object>(null, false, null);
            }
            else if (expr is MethodCallExpression exprCall)
            {
                if (exprCall.Method.GetCustomAttribute<IdempotentAttribute>() != null)
                {
                    //El atom evalua su argumento:
                    return EvalExprObj(exprCall.Arguments[0]);
                }
                else if (exprCall.Method.GetCustomAttribute<AlwaysThrowsAttribute>() != null)
                {
                    //Si el método siempre lanza excepción:
                    return new EvalExprResult<object>(null, false, null);
                }
                else if (exprCall.Method.GetCustomAttribute<AlwaysNullAttribute>() != null)
                {
                    //El método siempre evalue a null
                    return new EvalExprResult<object>(null, true, null);
                }
            }
            else if (expr is MemberExpression member)
            {
                var eval = EvalMemberExpr(member);
                if (eval != null)
                    return eval;
            }
            else if (expr is BinaryExpression exprBin)
            {
                var eval = EvalBinExpr(exprBin);
                if (eval != null)
                    return eval;
            }
            else if (expr is UnaryExpression exprUn)
            {
                var eval = EvalUnaryExpr(exprUn);
                if (eval != null)
                    return eval;
            }

            try
            {
                //La forma lenta pero segura de evaluar la expresión:
                var lambda = Expression.Lambda(expr, new ParameterExpression[0]);
                var comp = lambda.Compile();
                var ret = comp.DynamicInvoke(new object[0]);
                return new EvalExprResult<object>(ret, true, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(expr);
                return new EvalExprResult<object>(null, false, ex);
                //No se pudo evaluar:
                throw new ArgumentException($"No se pudo evaluar la expresión '{expr}'", ex);
            }
        }

        /// <summary>
        /// Trata de evaluar una expresión y devuelve el valor de la misma
        /// </summary>
        public static bool TryEvalExpr<T>(Expression expr, out T result)
        {
            var r = EvalExpr<T>(expr);
            if (r.Success)
            {
                result = r.Value;
            }
            else
            {
                result = default(T);
            }
            return r.Success;
        }

    }
}
