using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using KeaSql.ExprRewrite;
using KeaSql.Fluent;

namespace KeaSql.SqlText.Rewrite
{
    public static class SqlRewriteRules
    {
        public static string ToSql<T>(T expr) => throw new ArgumentException("Esta función no se puede llamar directamente");

        public static bool ExcludeFromRewrite(Expression expr)
        {
            //No hacemos el rewrite en los subqueries, esos ocupan su propio rewrite:
            return typeof(ISqlSelect).IsAssignableFrom(expr.Type);
        }

        /// <summary>
        /// Una llamada a una función de SQL
        /// </summary>
        public static T RawCall<T>(string func, params object[] args) => throw new ArgumentException("Esta función no se puede llamar directamente");

        /// <summary>
        /// Reglas basadas en los parámetros del select
        /// </summary>
        /// <param name="pars"></param>
        /// <returns></returns>
        public static IEnumerable<RewriteRule> ExprParamsRules(SqlExprParams pars)
        {
            var ret = new List<RewriteRule>();

            ret.Add(
                 RewriteRule.Create(
                    () => RewriteSpecialCalls.Call<string>(typeof(SqlRewriteRules), nameof(ToSql)),
                    null,
                    null,
                    (match, expr, visit) => Expression.Constant(SqlExpression.ExprToSql(((MethodCallExpression)expr).Arguments[0], pars, true)))
            );

            //Puede ser null el param en caso de que todo sea por sustituciones del replace
            if (pars.Param != null)
            {
                ret.Add(
                    new RewriteRule(Expression.Lambda(pars.Param), Expression.Lambda(Expression.Call(typeof(Sql), nameof(Sql.FromParam), new[] { pars.Param.Type })), null, null)
                    );
            }

            return ret;
        }

        /// <summary>
        /// Regla para las llamadas a RawCall
        /// </summary>
        public static RewriteRule rawCallRule = RewriteRule.Create(
            () => RewriteSpecialCalls.Call<object>(typeof(SqlRewriteRules), nameof(RawCall)),
            null,
            null,
            (match, expr, visit) =>
            {
                var call = expr as MethodCallExpression;
                var type = call.Method.GetGenericArguments()[0];
                var name = ((ConstantExpression)call.Arguments[0]).Value;
                var args = ((NewArrayExpression)call.Arguments[1]).Expressions.ToList();

                var concatExprs = new List<Expression>();
                concatExprs.Add(Expression.Constant(name + "("));
                for (var i = 0; i < args.Count; i++)
                {
                    var arg = args[i];
                    if (i > 0)
                    {
                        concatExprs.Add(Expression.Constant(", "));
                    }
                    concatExprs.Add(Expression.Call(typeof(SqlRewriteRules), "ToSql", new[] { arg.Type }, arg));
                }

                concatExprs.Add(Expression.Constant(")"));

                var retBody = concatExprs.Aggregate(DefaultRewrite.AddStr);
                var ret = Expression.Call(typeof(Sql), "Raw", new[] { type }, retBody);
                return ret;
            });

        /// <summary>
        /// Funciones de cadenas
        /// </summary>
        public static RewriteRule[] stringCalls = new[]
        {
            RewriteRule.Create(
                (string a, string b) => a.Contains(b),
                (a, b)  => Sql.Raw<bool>($"({ToSql(a)} LIKE '%' || {ToSql(b)} || '%')")),

            RewriteRule.Create(
                (string a, string b) => a.StartsWith(b),
                (a,b) => Sql.Raw<bool>($"({ToSql(a)} LIKE {ToSql(b)} || '%')")),

            RewriteRule.Create(
                (string a, string b) => a.EndsWith(b),
                (a,b) => Sql.Raw<bool>($"({ToSql(a)} LIKE '%' || {ToSql(b)})")),

            RewriteRule.Create (
                (string a, int si) => a.Substring(si),
                (a, si) => RawCall<string>("substr", a, si)),

            RewriteRule.Create (
                (string a, int si, int len) => a.Substring(si, len),
                (a, si,len) => RawCall<string>("substr", a, si, len)),

            RewriteRule.Create (
                (string a) => a.ToLower(),
                (a) => RawCall<string>("lower", a)),

            RewriteRule.Create (
                (string a) => a.ToUpper(),
                (a) => RawCall<string>("upper", a)),
        };
    }
}
