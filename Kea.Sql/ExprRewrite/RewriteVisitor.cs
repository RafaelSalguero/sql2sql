using System;
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
        readonly Func<Expression, bool> exclude;
        public RewriteVisitor(IEnumerable<RewriteRule> rules, Func<Expression, bool> exclude)
        {
            this.exclude = exclude;
            this.rules = rules;
        }

        public override Expression Visit(Expression node)
        {
            if (node == null) return null;
            if (exclude(node))
                return node;

            var ret = node;
            var ruleApplied = false;
            do
            {
                ruleApplied = false;
                foreach (var rule in rules)
                {
                    var apply = Rewriter.GlobalApplyRule(ret, rule, Visit);
                    if (apply != null)
                    {
                        ret = apply;
                        ruleApplied = true;
                    }
                }
            } while (ruleApplied);


            //Si se cambio algo en las subexpresiones, visitar de nuevo:
            if (!exclude(ret))
            {
                var subVisit = base.Visit(ret);
                if (subVisit != node)
                    return Visit(subVisit);
                return subVisit;
            }

            return ret;

        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            //Los parametros del lambda quedan intactos, sólo visitamos el body
            var body = this.Visit(node.Body);
            if (body == node.Body)
            {
                //No se modificó el body:
                return node;
            }

            return Expression.Lambda(body, node.Name, node.TailCall, node.Parameters);
        }
    }
}
