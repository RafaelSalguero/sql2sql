using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using KeaSql.ExprRewrite;

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
            SqlRewriteRules.rawCallRule
        }.Concat(SqlRewriteRules.stringCalls)
        ;

        readonly IEnumerable<RewriteRule> rules;
        public SqlRewriteVisitor(SqlExprParams pars)
        {
            rules = staticRules
                .Concat(SqlRewriteRules.ExprParamsRules(pars));
        }

        public LambdaExpression Visit(LambdaExpression  node)
        {
            return (LambdaExpression)Visit((Expression)node);
        }
        public override Expression Visit(Expression node)
        {
            var visitor = new RewriteVisitor(rules, SqlRewriteRules.ExcludeFromRewrite);
            var ret = node;
            ret = visitor.Visit(ret);

            return ret;
        }

    }
}
