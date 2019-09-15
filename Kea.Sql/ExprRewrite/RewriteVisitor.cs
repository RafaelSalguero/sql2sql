using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Sql2Sql.ExprRewrite
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
        public Expression After { get; set; }
        public double Time { get; }

        public List<RuleApplication> Applications { get; set; } = new List<RuleApplication>();
        public int TotalCount => 1 + Applications.Select(x => x.TotalCount).Sum();

        public override string ToString()
        {
            return $"{Rule.DebugName} ({TotalCount}) {Before} => {After}";
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

        /// <summary>
        /// Información de depuración, establezca la instancia para depurar, si no, establezca como null. Por default es null
        /// </summary>
        public static Stack<List<RuleApplication>> applications = null;

        public static int VisitCount;
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
                    var app = new RuleApplication(rule, ret, null, 0);
                    applications?.Push(app.Applications);
                    var apply = Rewriter.GlobalApplyRule(ret, rule, Visit);
                    applications?.Pop();

                    if (apply != ret)
                    {
                        app.After = apply;
                        applications?.Peek().Add(app);
                        ret = apply;
                        ruleApplied = true;
                    }
                }
            } while (ruleApplied);
            return ret;
        }

        public override Expression Visit(Expression node)
        {
            VisitCount++;
            if (node == null) return null;
            if (exclude(node))
                return node;

            //Primero aplica al nivel superior de la expresión:
            var ret = VisitTopLevel(node);

            if (!exclude(ret))
            {
                while (true)
                {
                    var subVisit = base.Visit(ret);
                    if(subVisit == ret)
                    {
                        return ret;
                    }

                    //Si el subVisit cambio la expresión, hay que seguir aplicando el topLevel y el subVisit hasta que no haya cambios:
                    var nextTop = VisitTopLevel(subVisit);
                    if(nextTop == subVisit)
                    {
                        return nextTop;
                    }
                    ret = nextTop;
                }
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
