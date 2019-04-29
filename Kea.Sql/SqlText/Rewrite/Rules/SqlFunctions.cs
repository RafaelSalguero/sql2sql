using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using KeaSql.ExprRewrite;
using KeaSql.Fluent.Data;

namespace KeaSql.SqlText.Rewrite.Rules
{
    public static class SqlFunctions
    {
        public static string ToSql<T>(T expr) => throw new ArgumentException("Esta función no se puede llamar directamente");
        public static string WindowToSql(ISqlWindow window) => throw new ArgumentException("Esta función no se puede llamar directamente");
        public static bool ExcludeFromRewrite(Expression expr)
        {
            //No hacemos el rewrite en los subqueries, esos ocupan su propio rewrite:
            if (typeof(ISqlSelect).IsAssignableFrom(expr.Type))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Una llamada a una función de SQL
        /// </summary>
        public static T RawCall<T>(string func, params object[] args) => throw new ArgumentException("Esta función no se puede llamar directamente");



        public static IEnumerable<RewriteRule> ExprParamsRules(SqlExprParams pars)
        {
            var ret = new List<RewriteRule>();
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
        /// La regla que se aplica a las llamadas Atom(Raw(x)), lo que hace es convertir las llamadas ToSql del Atom(Raw(x))
        /// </summary>
        public static IEnumerable<RewriteRule> AtomRawRule(SqlExprParams pars)
        {
            var toSqlRule = RewriteRule.Create(
                    () => RewriteSpecial.Call<string>(typeof(SqlFunctions), nameof(ToSql)),
                    null,
                    null,
                    (match, expr, visit) => Expression.Constant(SqlExpression.ExprToSql(((MethodCallExpression)expr).Arguments[0], pars, true)));

            var windowToSqlRule = RewriteRule.Create(
                   (ISqlWindow a) => WindowToSql(a),
                   null,
                   null,
                   (match, expr, visit) => Expression.Constant(SqlCalls.WindowToSql(match.Args[0]))
                   );
            var toSqlRules = new[]
            {
                toSqlRule,
                windowToSqlRule
            };
            Func<Expression, Expression> applySqlRule = (Expression ex) => new RewriteVisitor(toSqlRules, ExcludeFromRewrite).Visit(ex);

            var atomRawRule = RewriteRule.Create(
                (string x) => RewriteSpecial.Atom(Sql.Raw<RewriteTypes.C1>(x)),
                x => RewriteSpecial.Atom(Sql.Raw<RewriteTypes.C1>(RewriteSpecial.Transform(x, applySqlRule))),
                (match, expr) => applySqlRule(match.Args[0]) != match.Args[0]
                );

            return new[] { atomRawRule };
        }


        static Expression<Func<RewriteTypes.C1, RewriteTypes.C1>> isAtom = x => RewriteSpecial.Atom<RewriteTypes.C1>(x);

        public static RewriteRule[] rawAtom = new[] {
            RewriteRule.Create(
                (string x) => Sql.Raw<RewriteTypes.C1>(x),
                x => RewriteSpecial.Atom(Sql.Raw<RewriteTypes.C1>(x))
                )
        };


        /// <summary>
        /// Regla para las llamadas a RawCall
        /// </summary>
        public static RewriteRule rawCallRule = RewriteRule.Create(
            () => RewriteSpecial.Call<object>(typeof(SqlFunctions), nameof(RawCall)),
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
                    concatExprs.Add(Expression.Call(typeof(SqlFunctions), "ToSql", new[] { arg.Type }, arg));
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

            RewriteRule.Create(
                (string a) => a.Length,
                a => RawCall<int>("char_length", a)),
        };

        public static RewriteRule betweenRule = RewriteRule.Create(
                (RewriteTypes.C1 a, RewriteTypes.C1 min, RewriteTypes.C1 max) => Sql.Between(a, min, max),
                (a, min, max) => Sql.Raw<bool>($"{ToSql(a)} BETWEEN {ToSql(min)} {ToSql(max)}")
            );

        public static RewriteRule containsRule = RewriteRule.Create(
                (IEnumerable<RewriteTypes.C1> col, RewriteTypes.C1 item) => col.Contains(item),
                (col, it) => Sql.Raw<bool>($"({ToSql(it)} IN {ToSql(col)})")
            );

        public static RewriteRule[] sqlCalls = new[]
        {
            RewriteRule.Create(
                (RewriteTypes.C1 a, ISqlWindow over) => Sql.Over(a, over),
                (a, over) => Sql.Raw<RewriteTypes.C1>($"{ToSql(a)} OVER {WindowToSql(over)}")
            ),
            RewriteRule.Create(
                (RewriteTypes.C1 a, SqlType type) => Sql.Cast(a, type),
                (a, type) => Sql.Raw<RewriteTypes.C1>( $"CAST ({ToSql(a)} AS {type.Sql})")
            ),
            RewriteRule.Create(
                (string a, string b) => Sql.Like(a, b),
                (a,b) => Sql.Raw<bool>($"{ToSql(a)} LIKE {ToSql(b)}")
            ),
            RewriteRule.Create(
                (RewriteTypes.C1 a, bool b) => Sql.Filter(a, b),
                (a,b) => Sql.Raw<RewriteTypes.C1>($"{ToSql(a)} FILTER (WHERE {ToSql(b)})")
            ),

            containsRule,
            betweenRule
        };

      
    }
}
