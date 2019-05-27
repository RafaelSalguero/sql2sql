using System;
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
                    { ExpressionType.Modulo, "%" },

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
        /// Convierte la igualdad a null
        /// </summary>
        public static RewriteRule[] eqNullRule = new[] {
                RewriteRule.Create(
                "a == null",
                (RewriteTypes.C1 a) => a == null,
                (a) => Sql.Raw<bool>($"({SqlFunctions.ToSql(a)} IS NULL)")),

                RewriteRule.Create(
                "null == a",
                (RewriteTypes.C1 a) => null == a,
                (a) => Sql.Raw<bool>($"({SqlFunctions.ToSql(a)} IS NULL)")),

                 RewriteRule.Create(
                "a != null",
                (RewriteTypes.C1 a) => a != null,
                (a) => Sql.Raw<bool>($"({SqlFunctions.ToSql(a)} IS NOT NULL)")),

                 RewriteRule.Create(
                "null != a",
                (RewriteTypes.C1 a) => null != a,
                (a) => Sql.Raw<bool>($"({SqlFunctions.ToSql(a)} IS NOT NULL)")),
            };

        /// <summary>
        /// Regla para los operadores binarios
        /// </summary>
        public static RewriteRule[] binaryRules = new[] {
            RewriteRule.Create(
                "strAny a + b",
                (string a, RewriteTypes.C1 b) => a + b,
                (a,b) => Sql.Raw<string>("(" + SqlFunctions.ToSql(a) + " || " +  SqlFunctions.ToSql(b) + ")")
            ),

            RewriteRule.Create(
                "anyStr a + b",
                (RewriteTypes.C1 a, string b ) => a + b,
                (a,b) => Sql.Raw<string>("(" + SqlFunctions.ToSql(a) + " || " +  SqlFunctions.ToSql(b) + ")")
            ),

            RewriteRule.Create(
                "binaryOp",
                (RewriteTypes.C1 a, RewriteTypes.C2 b, ExpressionType op) => RewriteSpecial.Operator<RewriteTypes.C1, RewriteTypes.C2, RewriteTypes.C3>(a, b, op),
                (a, b, op)  => RewriteSpecial.Atom( Sql.Raw<RewriteTypes.C3>($"({SqlFunctions.ToSql(a)} {opNames[op]} {SqlFunctions.ToSql(b)})"))
                )
            };

        static string UnaryToSql(string operand, ExpressionType op)
        {
            switch (op)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    return $"-({operand})";
                case ExpressionType.Not:
                    return $"NOT ({operand})";
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    return operand;
            }
            throw new ArgumentException($"No se pudo convertir a SQL el operador unario '{op}'");
        }

        public static RewriteRule[] unaryRules = new[]
        {
            RewriteRule.Create(
                "convert",
                (RewriteTypes.C1 x) =>  RewriteSpecial.Operator<RewriteTypes.C1, RewriteTypes.C2>(x, ExpressionType.Convert),
                (a) => RewriteSpecial.Atom ( Sql.Raw<RewriteTypes.C2> (SqlFunctions.ToSql(a)))
                ),

            RewriteRule.Create(
                "unaryOp",
                (RewriteTypes.C1 x, ExpressionType op) =>  RewriteSpecial.Operator<RewriteTypes.C1, RewriteTypes.C2>(x, op),
                (a, op) => RewriteSpecial.Atom ( Sql.Raw<RewriteTypes.C2> (UnaryToSql(SqlFunctions.ToSql(a), op))),
                (match, expr) => expr.NodeType != ExpressionType.Convert
                ),
        };

        /// <summary>
        /// Reglas que convierten el HasValue y el Value de los Nullable
        /// </summary>
        public static RewriteRule[] nullableRules = new[]
        {
            RewriteRule.Create(
                "nullable.HasValue",
                (RewriteTypes.S1? a) => a.HasValue ,
                a => a != null
            ),
            RewriteRule.Create(
                "nullable.Value",
                (RewriteTypes.S1? a) => a.Value,
                a => Sql.Raw<RewriteTypes.S1>(SqlFunctions.ToSql(a))
            )
        };
    }
}
