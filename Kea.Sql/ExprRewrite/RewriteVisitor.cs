using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;

namespace KeaSql.ExprRewrite
{
    public class RuleApplication
    {
        public RuleApplication(RewriteRule rule, Expression before, Expression after, double time)
        {
            Rule = rule;
            Before = before;
            After = after;
            Time = time;
        }

        public RewriteRule Rule { get; }
        public Expression Before { get; }
        public Expression After { get; } 
        public double Time { get;  }
        public override string ToString()
        {
            return $"{Rule.DebugName} ({Time} ms)" ; 
        }
    }

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

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(RewriteSpecial) && node.Method.Name == nameof(RewriteSpecial.Atom))
            {
                //Es una expresión Atom, sólo evaluamos el primer nivel
                var ret = VisitTopLevel(node);
                return ret;
            }

            return base.VisitMethodCall(node);
        }

        Expression VisitTopLevel(Expression node)
        {
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


                    if (apply != ret)
                    {
                        var debugApp = new RuleApplication(rule, ret, apply, 0 );
                        ret = apply;
                        ruleApplied = true;
                    }
                }
            } while (ruleApplied);
            return ret;
        }

        public override Expression Visit(Expression node)
        {
            if (node == null) return null;
            var ret = VisitTopLevel(node);

            //Si se cambio algo en las subexpresiones, visitar de nuevo:
            if(ret == null)
            {
                ;
            }
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
