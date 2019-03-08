using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SqlToSql.SqlText
{
    public static class SqlExpression
    {
        /// <summary>
        /// Convierte una expresión a SQL
        /// </summary>
        public static string ExprToSql(Expression expr, Func<Expression, string> subStr)
        {
            string ToStr(Expression ex) => ExprToSql(ex, subStr);
            if (subStr(expr) is string strRep && strRep != null)
            {
                return strRep;
            }

            if (expr is BinaryExpression bin)
            {
                var ops = new Dictionary<ExpressionType, string>
                {
                    { ExpressionType.Equal, "=" }
                };

                if (ops.TryGetValue(bin.NodeType, out string opStr))
                {
                    return $"({ToStr(bin.Left)} {opStr} {ToStr(bin.Right)})";
                }
            }
            else if (expr is MemberExpression mem)
            {
                return $"{ToStr(mem.Expression)}.\"{mem.Member.Name}\"";
            }
            return expr.ToString();
        }
    }
}
