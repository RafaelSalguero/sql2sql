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
        public static readonly IEnumerable<RewriteRule> staticRules = new[]
        {
            DefaultRewrite.InvokeRule,
            DefaultRewrite.StringFormat,
            SqlFunctions.rawCallRule
        }

        .Concat(SqlOperators.eqNullRule)
        .Concat(SqlOperators.nullableRules)
        .Concat(SqlOperators.binaryRules)
        .Concat(SqlFunctions.stringCalls)
        ;

        readonly IEnumerable<RewriteRule> rules;
        public SqlRewriteVisitor(SqlExprParams pars)
        {
            rules = staticRules
                .Concat(SqlFunctions.ExprParamsRules(pars));
        }

        public LambdaExpression Visit(LambdaExpression node)
        {
            return (LambdaExpression)Visit((Expression)node);
        }
        public override Expression Visit(Expression node)
        {
            var visitor = new RewriteVisitor(rules, SqlFunctions.ExcludeFromRewrite);
            var ret = node;
            ret = visitor.Visit(ret);

            return ret;
        }

    }
}
