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
        public static readonly IEnumerable<RewriteRule> staticRules = 
        SqlFunctions.rawAtom.Concat(
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
        ;

        readonly IEnumerable<RewriteRule> rules;
        public SqlRewriteVisitor(SqlExprParams pars)
        {
            rules = staticRules
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
