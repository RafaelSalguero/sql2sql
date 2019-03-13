using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SqlToSql.Fluent;
using SqlToSql.PgLan;
using static SqlToSql.SqlText.SqlFromList;

namespace SqlToSql.SqlText
{
    public class SqlExprParams
    {
        public SqlExprParams(ParameterExpression param, ParameterExpression window, bool fromListNamed, string fromListAlias, IReadOnlyList<ExprStrAlias> replace)
        {
            Param = param;
            Window = window;
            FromListNamed = fromListNamed;
            FromListAlias = fromListAlias;
            Replace = replace;
        }

        public static SqlExprParams Empty => new SqlExprParams(null, null, false, "", new ExprStrAlias[0]);

        public SqlExprParams SetPars(ParameterExpression param, ParameterExpression window) =>
            new SqlExprParams(param, window, FromListNamed, FromListAlias, Replace);

        public ParameterExpression Param { get; }
        public ParameterExpression Window { get; }
        public bool FromListNamed { get; }
        public string FromListAlias { get; }
        public IReadOnlyList<ExprStrAlias> Replace { get; }
    }

    public static class SqlExpression
    {
       

     

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
                        return SqlCalls.RawToSql(call, pars);
                    case nameof(Sql.Over):
                        return SqlCalls.OverToSql(call, pars);
                }
            }
            else if (call.Method.DeclaringType == typeof(SqlExtensions))
            {
                switch(call.Method.Name)
                {
                    case nameof(SqlExtensions.Scalar):
                        return SqlCalls.ScalarToSql(call, pars);
                }
                throw new ArgumentException("Para utilizar un subquery dentro de una expresión utilice la función SqlExtensions.Scalar");
            }

            throw new ArgumentException("No se pudo convertir a SQL la llamada a la función " + call);
        }

        public static string ExprToSql(Expression expr, SqlExprParams pars)
        {
            return ExprToSqlStar(expr, pars).sql;
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

        static (string sql, bool star) MemberToSql(MemberExpression mem, SqlExprParams pars)
        {
            if (pars.FromListNamed)
            {
                MemberExpression firstExpr = mem;
                while (firstExpr is MemberExpression sm1 && sm1.Expression is MemberExpression sm2)
                {
                    firstExpr = sm2;
                }

                if (mem.Expression == pars.Param)
                {
                    return ($"\"{mem.Member.Name}\".*", true);
                }
                else if (firstExpr.Expression == pars.Param)
                {
                    return ($"\"{firstExpr.Member.Name}\".\"{mem.Member.Name}\"", false);
                }
            }
            else
            {
                Expression firstExpr = mem;
                while (firstExpr is MemberExpression sm)
                {
                    firstExpr = sm.Expression;
                }

                if (firstExpr == pars.Param)
                {
                    return ($"{pars.FromListAlias}.\"{mem.Member.Name}\"", false);
                }
            }

            //Intentamos convertir al Expr a string con el replace:
            var exprRep = SqlFromList.ReplaceStringAliasMembers(mem.Expression, pars.Replace);
            if (exprRep != null)
            {
                return ($"{exprRep}.\"{mem.Member.Name}\"", false);
            }

            var exprStr = ExprToSql(mem.Expression, pars);
            return ($"{exprStr}.\"{mem.Member.Name}\"", false);
        }

        /// <summary>
        /// Convierte una expresión a SQL
        /// </summary>
        public static (string sql, bool star) ExprToSqlStar(Expression expr, SqlExprParams pars)
        {
            //Es importante primero comprobar la igualdad del parametro, ya que el replace list tiene una entrada para el parametro tambien
            if (expr == pars.Param)
            {
                if (pars.FromListNamed)
                {
                    return ($"*", true);
                }

                return ($"{pars.FromListAlias}.*", true);
            }

            var replace = SqlFromList.ReplaceStringAliasMembers(expr, pars.Replace);
            if (replace != null) return (replace, false);

            string ToStr(Expression ex) => ExprToSql(ex, pars);

            if (expr is BinaryExpression bin)
            {
                return (BinaryToSql(bin, pars), false);
            }
            
            else if (expr is MemberExpression mem)
            {
                return MemberToSql(mem, pars);
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
