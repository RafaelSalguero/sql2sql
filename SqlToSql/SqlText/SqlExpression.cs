using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SqlToSql.Fluent;
using SqlToSql.PgLan;

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

        public static SqlExprParams Empty => new SqlExprParams(null, null, false, "", null);

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

        static string CastToSql(MethodCallExpression call, SqlExprParams pars)
        {
            var type = Expression.Lambda<Func<SqlType>>(call.Arguments[1]).Compile()();

            return $"CAST ({ExprToSql(call.Arguments[0], pars)} AS {type.Sql})";
        }

        static string OverToSql(MethodCallExpression call, SqlExprParams pars)
        {
            return $"{ExprToSql(call.Arguments[0], pars)} OVER {WindowToSql(call.Arguments[1])}";
        }

        static string CallToSql(MethodCallExpression call, SqlExprParams pars)
        {
            var funcAtt = call.Method.GetCustomAttribute<SqlNameAttributeAttribute>();
            if (funcAtt != null)
            {
                var args = string.Join(", ", call.Arguments.Select(x => ExprToSql(x, pars)));
                return $"{funcAtt.SqlName}({args})";
            }
            else if (call.Method.DeclaringType == typeof(Sql))
            {
                switch (call.Method.Name)
                {
                    case nameof(Sql.Raw):
                        return RawToSql(call, pars);
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

        static string RawToSql(MethodCallExpression call, SqlExprParams pars)
        {
            if (call.Arguments[0] is ConstantExpression con)
            {
                return con.Value.ToString();
            }
            throw new ArgumentException("El SQL raw debe de ser una cadena constante");
        }

        public static string ConditionalToSql(ConditionalExpression expr, SqlExprParams pars)
        {
            var b = new StringBuilder();
            b.AppendLine("CASE");

            Expression curr = expr;

            while (curr is ConditionalExpression cond)
            {
                b.Append("WHEN ");
                b.Append(ExprToSql(cond.Test, pars));
                b.Append(" THEN ");
                b.Append(ExprToSql(cond.IfTrue, pars));

                b.AppendLine();
                curr = cond.IfFalse;
            }

            b.Append("ELSE ");
            b.Append(ExprToSql(curr, pars));
            b.AppendLine();
            b.AppendLine("END");

            return b.ToString();
        }

        static string ConstToSql(object value)
        {
            if (value == null)
            {
                return "NULL";
            }
            else if (value is string)
            {
                return $"'{value}'";
            }
            else if (
                value is decimal || value is int || value is float || value is double || value is long || value is byte || value is sbyte ||
                value is bool
                )
            {
                return value.ToString();
            }
            throw new ArgumentException($"No se puede convertir a SQL la constante " + value.ToString());
        }

        static string BinaryToSql(BinaryExpression bin, SqlExprParams pars)
        {
            string ToStr(Expression ex) => ExprToSql(ex, pars);
            if (bin.Right is ConstantExpression conR && conR.Value == null)
            {
                if (bin.NodeType == ExpressionType.Equal)
                    return $"({ToStr(bin.Left)} IS NULL)";
                else if (bin.NodeType == ExpressionType.NotEqual)
                    return $"({ToStr(bin.Left)} IS NOT NULL)";
                else
                    throw new ArgumentException("No se puede convertir la expresión " + bin);
            }
            else if (bin.Left is ConstantExpression conL && conL.Value == null)
            {
                if (bin.NodeType == ExpressionType.Equal)
                    return $"({ToStr(bin.Right)} IS NULL)";
                else if (bin.NodeType == ExpressionType.NotEqual)
                    return $"({ToStr(bin.Right)} IS NOT NULL)";
                else
                    throw new ArgumentException("No se puede convertir la expresión " + bin);
            }


            var ops = new Dictionary<ExpressionType, string>
                {
                    { ExpressionType.Add, "+" },
                    { ExpressionType.AddChecked, "+" },

                    { ExpressionType.Subtract, "-" },
                    { ExpressionType.SubtractChecked, "-" },

                    { ExpressionType.Multiply, "*" },
                    { ExpressionType.MultiplyChecked, "*" },

                    { ExpressionType.Divide, "/" },

                    { ExpressionType.Equal, "=" },
                    { ExpressionType.NotEqual, "!=" },
                    { ExpressionType.GreaterThan, ">" },
                    { ExpressionType.GreaterThanOrEqual, ">=" },
                    { ExpressionType.LessThan, "<" },
                    { ExpressionType.LessThanOrEqual, "<=" },

                    { ExpressionType.AndAlso, "AND" },
                    { ExpressionType.OrElse, "OR" },
                };

            if (ops.TryGetValue(bin.NodeType, out string opStr))
            {
                return $"({ToStr(bin.Left)} {opStr} {ToStr(bin.Right)})";
            }
            throw new ArgumentException("No se pudo convertir a SQL la expresión binaria" + bin);
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
                return (BinaryToSql(bin, pars), false);
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
                if (exprRep != null)
                {
                    return ($"{exprRep}.\"{mem.Member.Name}\"", false);
                }

                return ($"{ToStr(mem.Expression)}.\"{mem.Member.Name}\"", false);
            }
            else if (expr is ConditionalExpression cond)
            {
                return (ConditionalToSql(cond, pars), false);
            }
            else if (expr is MethodCallExpression call)
            {
                return (CallToSql(call, pars), false);
            }
            else if (expr is ConstantExpression cons)
            {
                return (ConstToSql(cons.Value), false);
            }
            throw new ArgumentException("No se pudo convertir a SQL la expresión " + expr.ToString());
        }
    }
}
