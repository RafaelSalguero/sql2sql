using System.Collections.Generic;
using System.Linq.Expressions;

namespace KeaSql.ExprRewrite
{
    /// <summary>
    /// Aplica recursivamente un conjunto de <see cref="RewriteRule"/>
    /// </summary>
    public class RewriteVisitor : ExpressionVisitor
    {
        readonly IEnumerable<RewriteRule> rules;
        public RewriteVisitor(IEnumerable<RewriteRule> rules)
        {
            this.rules = rules;
        }

        public override Expression Visit(Expression node)
        {
            var ret = node;
            var ruleApplied = false;
            do
            {
                ruleApplied = false;
                foreach (var rule in rules)
                {
                    var apply = Rewriter.GlobalApplyRule(ret, rule);
                    if (apply != null)
                    {
                        ret = apply;
                        ruleApplied = true;
                    }
                }
            } while (ruleApplied);


            //Si se cambio algo en las subexpresiones, visitar de nuevo:
            var subVisit = base.Visit(ret);
            if (subVisit != node)
                return Visit(subVisit);

            return subVisit;
        }
    }
}
