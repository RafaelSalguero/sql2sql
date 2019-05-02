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
                    new RewriteRule(
                        "fromParam",
                        Expression.Lambda(pars.Param), Expression.Lambda(Expression.Call(typeof(Sql), nameof(Sql.FromParam), new[] { pars.Param.Type })), null, null)
                    );
            }
            return ret;
        }

        public static IEnumerable<RewriteRule> AtomInvokeParam(SqlExprParams pars)
        {
            var invokeRule = RewriteRule.Create(
             "atomInvokeParam",
             () => RewriteSpecial.Call<RewriteTypes.C2>(null, "Invoke"),
             null,
             null,
             (match, expr, visit) =>
             {
                 var exprCall = (MethodCallExpression)expr;
                 var origArgs = exprCall.Arguments;

                 //Aplicar la transformación del from a los parametros, esto es importante porque el from
                 //en algunos casos se usa sólo para especificar el tipo en un método generico pero su valor 
                 //no es importante.
                 var fromArgs = origArgs
                     .Select(x => Rewriter.RecApplyRules(x, ExprParamsRules(pars), ExcludeFromRewrite))
                     .ToList()
                     ;

                 var arg = fromArgs[0];
                 var args = fromArgs;

                 var atomArg = Expression.Call(typeof(RewriteSpecial), "Atom", new[] { arg.Type }, arg);
                 var atomArgs = new[] { atomArg }.Concat(args.Skip(1)).ToList();



                 var retCall = Expression.Call(exprCall.Object, exprCall.Method, atomArgs);
                 return retCall;

             });

            return new[] { invokeRule };
        }

        public static RewriteRule[] toSqlAtomRaw = new[] {
            RewriteRule.Create("toSqlAtomRaw",
            (string x) => SqlFunctions.ToSql<RewriteTypes.C1>(RewriteSpecial.Atom<RewriteTypes.C1>(Sql.Raw<RewriteTypes.C1>(x))),
            x => RewriteSpecial.Atom(x)
            ),

            RewriteRule.Create("toSqlAtomRawRowRef",
            (string x) => SqlFunctions.ToSql<RewriteTypes.C1>(RewriteSpecial.Atom<RewriteTypes.C1>(Sql.RawRowRef<RewriteTypes.C1>(x))),
            x => RewriteSpecial.Atom(x)
            )

            };


        public static SqlExprParams ExprParams() => throw new ArgumentException("No se puede llamar ");

        public static RewriteRule ToSqlRule(SqlExprParams pars) =>
        RewriteRule.Create(
                 "toSql",
                 (RewriteTypes.C1 x) => SqlFunctions.ToSql(x),
                 
                 null,
                 (match, expr, visit) => Expression.Constant(SqlExpression.ExprToSql(((MethodCallExpression)expr).Arguments[0], pars, true)));


        /// <summary>
        /// La regla que se aplica a las llamadas Atom(Raw(x)), lo que hace es convertir las llamadas ToSql del Atom(Raw(x))
        /// </summary>
        public static IEnumerable<RewriteRule> AtomRawRule(SqlExprParams pars)
        {
            var deferredToSqlRule = RewriteRule.Create(
                    "deferredToSql",
                    (RewriteTypes.C1 x) => SqlFunctions.ToSql<RewriteTypes.C1>(x),
                    null,
                    null,
                    (match, expr, visit) => Expression.Call(
                        typeof(SqlExpression),
                        nameof(SqlExpression.ExprToSql),
                        new Type[0],
                        Expression.Constant(match.Args[0]),
                        Expression.Constant(pars),
                        Expression.Constant(true)));

            var toSqlRule = RewriteRule.Create(
                 "toSql",
                 () => RewriteSpecial.Call<string>(typeof(SqlFunctions), nameof(ToSql)),
                 null,
                 null,
                 (match, expr, visit) => Expression.Constant(SqlExpression.ExprToSql(((MethodCallExpression)expr).Arguments[0], pars, false)));


            var windowToSqlRule = RewriteRule.Create(
                    "windowToSql",
                   (ISqlWindow a) => WindowToSql(a),
                   null,
                   null,
                   (match, expr, visit) => Expression.Constant(SqlCalls.WindowToSql(match.Args[0]))
                   );
            var toSqlRules =
                toSqlAtomRaw.Concat(
                new[]
                {
                    toSqlRule,
                    windowToSqlRule
                });
            Func<Expression, Expression> applySqlRule = (Expression ex) => new RewriteVisitor(toSqlRules, ExcludeFromRewrite).Visit(ex);

            var atomRawRule = RewriteRule.Create(
                "applySqlToAtomRaw",
                (string x) => RewriteSpecial.Atom(Sql.Raw<RewriteTypes.C1>(x)),
                null,
                null,
                (match, expr, visit) =>
                {
                    var arg = match.Args[0];
                    var toSql = applySqlRule(arg);
                    if (toSql != arg)
                    {
                        var type = match.Types[typeof(RewriteTypes.C1)];
                        //Si aplico el toSql, devolver una expresión diferente:
                        var ret = Expression.Call(typeof(RewriteSpecial), nameof(RewriteSpecial.Atom), new[] { type },
                            Expression.Call(typeof(Sql), nameof(Sql.Raw), new[] { type }, toSql)
                            );
                        return ret;
                    }
                    else
                    {
                        return expr;
                    }
                });

            return new[] { atomRawRule };
        }


        static Expression<Func<RewriteTypes.C1, RewriteTypes.C1>> isAtom = x => RewriteSpecial.Atom<RewriteTypes.C1>(x);



        public static RewriteRule[] rawAtom = new[] {
            RewriteRule.Create(
                "atomRawRowRef",
                (string x) => Sql.RawRowRef<RewriteTypes.C1>(x),
                x => RewriteSpecial.Atom(Sql.RawRowRef<RewriteTypes.C1>(x))
                ),

            RewriteRule.Create(
                "atomRawTableRef",
                (string x) => Sql.RawTableRef<RewriteTypes.C1>(x),
                x => RewriteSpecial.Atom(Sql.RawTableRef<RewriteTypes.C1>(x))
                ),
             RewriteRule.Create(
                "atomRawSubquery",
                (string x) => Sql.RawSubquery<RewriteTypes.C1>(x),
                x => RewriteSpecial.Atom(Sql.RawSubquery<RewriteTypes.C1>(x))
                )
        };


        /// <summary>
        /// Regla para las llamadas a RawCall
        /// </summary>
        public static RewriteRule rawCallRule = RewriteRule.Create(
            "rawCall",
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
                "strContains",
                (string a, string b) => a.Contains(b),
                (a, b)  => Sql.Raw<bool>($"({RewriteSpecial.Visit(ToSql(a))} LIKE '%' || {RewriteSpecial.Visit(ToSql(b))} || '%')")),

            RewriteRule.Create(
                "strStartsWith",
                (string a, string b) => a.StartsWith(b),
                (a,b) => Sql.Raw<bool>($"({ToSql(a)} LIKE {ToSql(b)} || '%')")),

            RewriteRule.Create(
                "strEndsWith",
                (string a, string b) => a.EndsWith(b),
                (a,b) => Sql.Raw<bool>($"({ToSql(a)} LIKE '%' || {ToSql(b)})")),

            RewriteRule.Create (
                "strSubstring1",
                (string a, int si) => a.Substring(si),
                (a, si) => RawCall<string>("substr", a, si)),

            RewriteRule.Create (
                "strSubstring2",
                (string a, int si, int len) => a.Substring(si, len),
                (a, si,len) => RawCall<string>("substr", a, si, len)),

            RewriteRule.Create (
                "strLower",
                (string a) => a.ToLower(),
                (a) => RawCall<string>("lower", a)),

            RewriteRule.Create (
                "strUpper",
                (string a) => a.ToUpper(),
                (a) => RawCall<string>("upper", a)),

            RewriteRule.Create(
                "strLength",
                (string a) => a.Length,
                a => RawCall<int>("char_length", a)),
        };

        public static RewriteRule betweenRule = RewriteRule.Create(
                "sqlBetween",
                (RewriteTypes.C1 a, RewriteTypes.C1 min, RewriteTypes.C1 max) => Sql.Between(a, min, max),
                (a, min, max) => Sql.Raw<bool>($"{ToSql(a)} BETWEEN {ToSql(min)} {ToSql(max)}")
            );

        public static RewriteRule containsRule = RewriteRule.Create(
                "sqlIn",
                (IEnumerable<RewriteTypes.C1> col, RewriteTypes.C1 item) => col.Contains(item),
                (col, it) => Sql.Raw<bool>($"({ToSql(it)} IN {ToSql(Sql.Record(col))})")
            );

        public static RewriteRule recordRule = RewriteRule.Create(
                "sqlRecord",
                (IEnumerable<RewriteTypes.C1> x) => Sql.Record(x),
                x => Sql.Raw<IEnumerable<RewriteTypes.C1>>($"({string.Join(", ", x.Select(y => SqlConst.ConstToSql(y)))  })")
                );

        public static RewriteRule[] sqlCalls = new[]
        {
            RewriteRule.Create(
                "sqlOver",
                (RewriteTypes.C1 a, ISqlWindow over) => Sql.Over(a, over),
                (a, over) => Sql.Raw<RewriteTypes.C1>($"{ToSql(a)} OVER {WindowToSql(over)}")
            ),
            RewriteRule.Create(
                "sqlCast",
                (RewriteTypes.C1 a, SqlType type) => Sql.Cast(a, type),
                (a, type) => Sql.Raw<RewriteTypes.C1>( $"CAST ({ToSql(a)} AS {type.Sql})")
            ),
            RewriteRule.Create(
                "sqlLike",
                (string a, string b) => Sql.Like(a, b),
                (a,b) => Sql.Raw<bool>($"{ToSql(a)} LIKE {ToSql(b)}")
            ),
            RewriteRule.Create(
                "sqlFilter",
                (RewriteTypes.C1 a, bool b) => Sql.Filter(a, b),
                (a,b) => Sql.Raw<RewriteTypes.C1>($"{ToSql(a)} FILTER (WHERE {ToSql(b)})")
            ),

            recordRule ,
            containsRule,
            betweenRule
        };


    }
}
