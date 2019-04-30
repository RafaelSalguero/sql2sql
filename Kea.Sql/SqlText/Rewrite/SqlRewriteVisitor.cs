using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using KeaSql.ExprRewrite;
using KeaSql.SqlText.Rewrite.Rules;

namespace KeaSql.SqlText.Rewrite
{
    /// <summary>
    /// Expande los .Invoke de las expresiones y aplica las reglas del SqlRewrite
    /// </summary>
    public class SqlRewriteVisitor : ExpressionVisitor
    {
        readonly IEnumerable<RewriteRule> rules;
        public SqlRewriteVisitor(SqlExprParams pars)
        {
            rules =
                SqlFunctions.rawAtom
                .Concat(SqlFunctions.AtomInvokeParam(pars))
                .Concat(
                    new[]
                    {
                        SqlConst.constToSqlRule,
                        DefaultRewrite.InvokeRule,
                        DefaultRewrite.StringFormat,
                        SqlFunctions.rawCallRule
                    }
                )
                .Concat(SqlOperators.eqNullRule)
                .Concat(SqlOperators.nullableRules)
                .Concat(SqlOperators.unaryRules)
                .Concat(SqlOperators.binaryRules)
                .Concat(SqlFunctions.stringCalls)
                .Concat(SqlFunctions.sqlCalls)
                .Concat(SqlFunctions.ExprParamsRules(pars))
                .Concat(SqlFunctions.AtomRawRule(pars))
                ;
        }

        public LambdaExpression Visit(LambdaExpression node)
        {
            return (LambdaExpression)Visit((Expression)node);
        }
        public override Expression Visit(Expression node)
        {
            return Rewriter.RecApplyRules(node, rules, SqlFunctions.ExcludeFromRewrite);
        }

    }
}
