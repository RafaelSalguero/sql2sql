using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sql2Sql.ExprRewrite;
using Sql2Sql.SqlText.Rewrite.Rules;

namespace Sql2Sql.SqlText.Rewrite
{
    /// <summary>
    /// Expande los .Invoke de las expresiones y aplica las reglas del SqlRewrite
    /// </summary>
    public class SqlRewriteVisitor : ExpressionVisitor
    {
        /// <summary>
        /// Lista de los pases de reglas
        /// </summary>
        readonly List<IEnumerable<RewriteRule>> rules;
        public SqlRewriteVisitor(SqlExprParams pars)
        {
            var exprParamRules = SqlFunctions.ExprParamsRules(pars);
            rules = new List<IEnumerable<RewriteRule>>();
            //Primero quita los invokes
            rules.Add(
                new[]
                {
                     DefaultRewrite.InvokeRule(exprParamRules)
                });

            rules.Add(DefaultRewrite.BooleanSimplify);

            rules.Add(
                exprParamRules
                );

            rules.Add(
                SqlFunctions.rawAtom
                .Concat(SqlFunctions.AtomInvokeParam(pars))
                .Concat(
                    new[]
                    {
                        SqlConst.constToSqlRule,
                        DefaultRewrite.StringFormat,
                        SqlFunctions.rawCallRule
                    }
                )
                .Concat(SqlOperators.eqNullRule)
                .Concat(SqlOperators.nullableRules)
                .Concat(SqlOperators.unaryRules)
                .Concat(SqlOperators.binaryRules)
                .Concat(SqlOperators.compareTo)
                .Concat(SqlFunctions.stringCalls)
                .Concat(SqlFunctions.subqueryExprs)
                .Concat(SqlFunctions.sqlCalls)
                .Concat(SqlFunctions.AtomRawRule(pars))
                .ToList()
                )
                ;
        }

        public LambdaExpression Visit(LambdaExpression node)
        {
            return (LambdaExpression)Visit((Expression)node);
        }
        public override Expression Visit(Expression node)
        {
            var ret = node;
            foreach (var ruleSet in rules)
            {
                var old = ret;
                var next = Rewriter.RecApplyRules(old, ruleSet, SqlFunctions.ExcludeFromRewrite);
                ret = next;
            }
            return ret;
        }

        static bool FromItemExcludeFromRewrite(Expression expr)
        {
            //No hacemos el rewrite en los subqueries, esos ocupan su propio rewrite:
            if (!typeof(ISqlSelect).IsAssignableFrom(expr.Type))
            {
                return true;
            }

            return false;
        }

        public static Expression VisitFromItem(Expression expr)
        {
            var ret = Rewriter.RecApplyRules(expr, new[] { DefaultRewrite.InvokeRule(null) }, x => false);
            return ret;
        }

    }
}
