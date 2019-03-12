using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SqlToSql.Fluent;

namespace SqlToSql.SqlText
{
    public class SqlExprParams
    {
        public SqlExprParams(ParameterExpression param, ParameterExpression window, bool fromListNamed, string fromListAlias, Func<Expression, string> replace)
        {
            Param = param;
            Window = window;
            FromListNamed = fromListNamed;
            FromListAlias = fromListAlias;
            Replace = replace;
        }

        public SqlExprParams SetPars(ParameterExpression param, ParameterExpression window) =>
            new SqlExprParams(param, window, FromListNamed, FromListAlias, Replace);

        public ParameterExpression Param { get; }
        public ParameterExpression Window { get; }
        public bool FromListNamed { get; }
        public string FromListAlias { get; }
        public Func<Expression, string> Replace { get; }
    }

    public static class SqlExpression
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

        static string OverToSql(MethodCallExpression call, SqlExprParams pars)
        {
            return $"{ExprToSql(call.Arguments[0], pars)} OVER {WindowToSql(call.Arguments[1])}";
        }

        static string CallToSql(MethodCallExpression call, SqlExprParams pars)
        {
            var funcAtt = call.Method.GetCustomAttribute<SqlFunctionAttribute>();
            if (funcAtt != null)
            {
                var args = string.Join(", ", call.Arguments.Select(x => ExprToSql(x, pars)));
                return $"{funcAtt.SqlName}({args})";
            }
            else if (call.Method.DeclaringType == typeof(Sql))
            {
                switch (call.Method.Name)
                {
                    case nameof(Sql.Over):
                        return OverToSql(call, pars);
                }
            }

            throw new ArgumentException("No se pudo convertir a SQL la llamada a la función " + call);
        }

        public static string ExprToSql(Expression expr, SqlExprParams pars)
        {
            return ExprToSqlStar(expr, pars).sql;
        }
        /// <summary>
        /// Convierte una expresión a SQL
        /// </summary>
        public static (string sql, bool star) ExprToSqlStar(Expression expr, SqlExprParams pars)
        {
            var replace = pars.Replace?.Invoke(expr);
            if (replace != null) return (replace, false);

            string ToStr(Expression ex) => ExprToSql(ex, pars);

            if (expr is BinaryExpression bin)
            {
                var ops = new Dictionary<ExpressionType, string>
                {
                    { ExpressionType.Equal, "=" }
                };

                if (ops.TryGetValue(bin.NodeType, out string opStr))
                {
                    return ($"({ToStr(bin.Left)} {opStr} {ToStr(bin.Right)})", false);
                }
            }
            else if (expr == pars.Param)
            {
                return ("*", true);
            }
            else if (expr is MemberExpression mem)
            {
                if (pars.FromListNamed)
                {
                    if (mem.Expression == pars.Param)
                    {
                        return ($"\"{mem.Member.Name}\".*", true);
                    }
                    else if (mem.Expression is MemberExpression mem2 && mem2.Expression == pars.Param)
                    {
                        return ($"\"{mem2.Member.Name}\".\"{mem.Member.Name}\"", false);
                    }
                }
                else
                {
                    if (mem.Expression == pars.Param)
                    {
                        return ($"\"{mem.Member.Name}\"", false);
                    }
                }

                //Intentamos convertir al Expr a string con el replace:
                var exprRep = pars.Replace?.Invoke(mem.Expression);
                if(exprRep != null)
                {
                    return ($"{exprRep}.\"{mem.Member.Name}\"", false);
                }
                throw new ArgumentException("No se pudo convertir a SQL el miembro " + expr.ToString());
            }
            else if (expr is MethodCallExpression call)
            {
                return (CallToSql(call, pars), false);
            }
            throw new ArgumentException("No se pudo convertir a SQL la expresión " + expr.ToString());
        }
    }
}
