using System;
using System.Linq.Expressions;
using KeaSql.Fluent;

namespace KeaSql.SqlText
{
    /// <summary>
    /// Convierte llamadas a funciones especiales a SQL
    /// </summary>
    public static class SqlCalls
    {
        public static string WindowToSql(Expression ex)
        {
            if (ex is MemberExpression mem)
            {
                return $"\"{mem.Member.Name}\"";
            }
            else
                throw new ArgumentException("No se pudo convertir a un WINDOW la expresión " + ex.ToString());
        }

        public static string RawToSql(MethodCallExpression call, SqlExprParams pars)
        {
            var arg = call.Arguments[0];

            if (!ExprRewrite.ExprEval.TryEvalExpr(arg, out object result))
                throw new ArgumentException($"No se pudo evaluar el contenido del Sql.Raw '{arg}'");

            return (string)result;
        }

        public static string CastToSql(MethodCallExpression call, SqlExprParams pars)
        {
            var type = Expression.Lambda<Func<SqlType>>(call.Arguments[1]).Compile()();

            return $"CAST ({SqlExpression.ExprToSql(call.Arguments[0], pars, false)} AS {type.Sql})";
        }

        public static string OverToSql(MethodCallExpression call, SqlExprParams pars)
        {
            return $"{SqlExpression.ExprToSql(call.Arguments[0], pars, false)} OVER {WindowToSql(call.Arguments[1])}";
        }

        public static string LikeToSql(MethodCallExpression call, SqlExprParams pars)
        {
            return $"{SqlExpression.ExprToSql(call.Arguments[0], pars, false)} LIKE {SqlExpression.ExprToSql(call.Arguments[1], pars, false)}";
        }

        public static string FilterToSql(MethodCallExpression call, SqlExprParams pars)
        {
            return $"{SqlExpression.ExprToSql(call.Arguments[0], pars, false)} FILTER (WHERE {SqlExpression.ExprToSql(call.Arguments[1], pars, false)})";
        }

        public static string BetweenToSql(MethodCallExpression call, SqlExprParams pars)
        {
            return $"({SqlExpression.ExprToSql(call.Arguments[0], pars, false)} BETWEEN {SqlExpression.ExprToSql(call.Arguments[1], pars, false)} AND {SqlExpression.ExprToSql(call.Arguments[2], pars, false)})";
        }

        public static string ScalarToSql(MethodCallExpression call, SqlExprParams pars)
        {
            var selectCall = call.Arguments[0];
            return SubqueryToSql(selectCall, pars);
        }

        public static string SubqueryToSql(Expression expr, SqlExprParams pars)
        {
            var selectCall = expr;
            var callSub = SqlFromList.ReplaceSubqueryBody(selectCall, pars.Replace);
            var subqueryFunc = Expression.Lambda(callSub).Compile();
            var subqueryExec = (ISqlSelectExpr)subqueryFunc.DynamicInvoke(new object[0]);

            var selectStr = SqlSelect.TabStr( SqlSelect.SelectToString(subqueryExec.Clause, pars.ParamMode, pars.ParamDic));
            return $"(\r\n{selectStr}\r\n)";
        }
    }
}
