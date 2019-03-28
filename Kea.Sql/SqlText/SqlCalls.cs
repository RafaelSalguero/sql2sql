using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using KeaSql.Fluent;

namespace KeaSql.SqlText
{
    /// <summary>
    /// Convierte llamadas a funciones especiales a SQL
    /// </summary>
    public static class SqlCalls
    {
        static string WindowToSql(Expression ex)
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
            if (call.Arguments[0] is ConstantExpression con)
            {
                return con.Value.ToString();
            }
            throw new ArgumentException("El SQL raw debe de ser una cadena constante");
        }

        public static string CastToSql(MethodCallExpression call, SqlExprParams pars)
        {
            var type = Expression.Lambda<Func<SqlType>>(call.Arguments[1]).Compile()();

            return $"CAST ({SqlExpression.ExprToSql(call.Arguments[0], pars)} AS {type.Sql})";
        }

        public static string OverToSql(MethodCallExpression call, SqlExprParams pars)
        {
            return $"{SqlExpression.ExprToSql(call.Arguments[0], pars)} OVER {WindowToSql(call.Arguments[1])}";
        }

        public static string FilterToSql(MethodCallExpression call, SqlExprParams pars)
        {
            return $"{SqlExpression.ExprToSql(call.Arguments[0], pars)} FILTER (WHERE {SqlExpression.ExprToSql(call.Arguments[1], pars)})";
        }

        public static string BetweenToSql(MethodCallExpression call, SqlExprParams pars)
        {
            return $"({SqlExpression.ExprToSql(call.Arguments[0], pars)} BETWEEN {SqlExpression.ExprToSql(call.Arguments[1], pars)} AND {SqlExpression.ExprToSql(call.Arguments[2], pars)})";
        }

        public static string ScalarToSql(MethodCallExpression call, SqlExprParams pars)
        {
            var selectCall = call.Arguments[0];
            var callSub = SqlFromList.ReplaceSubqueryBody(selectCall, pars.Replace);
            var subqueryFunc = Expression.Lambda(callSub).Compile();
            var subqueryExec = (ISqlSelect)subqueryFunc.DynamicInvoke(new object[0]);

            var selectStr = SqlSelect.SelectToString(subqueryExec.Clause, pars.ParamMode, pars.ParamDic);
            return $"({selectStr})";
        }
    }
}
