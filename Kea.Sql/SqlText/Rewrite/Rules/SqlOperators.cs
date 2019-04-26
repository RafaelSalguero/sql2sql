using System.Collections.Generic;
using System.Linq.Expressions;
using KeaSql.ExprRewrite;

namespace KeaSql.SqlText.Rewrite.Rules
{
    public class SqlOperators
    {

        static readonly Dictionary<ExpressionType, string> opNames = new Dictionary<ExpressionType, string>
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

        /// <summary>
        /// Regla para los operadores binarios
        /// </summary>
        public static RewriteRule binaryRule = RewriteRule.Create(
            (RewriteSpecial.Type1 a, RewriteSpecial.Type2 b, ExpressionType op) => RewriteSpecial.Operator<RewriteSpecial.Type1, RewriteSpecial.Type2, RewriteSpecial.Type3>(a, b, op),
            (a, b, op) => Sql.Raw<RewriteSpecial.Type3>($"{SqlFunctions.ToSql(a)} {opNames[op]} {SqlFunctions.ToSql(b)}")
            );

    }
}
