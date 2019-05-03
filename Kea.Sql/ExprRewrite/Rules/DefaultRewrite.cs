using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using KeaSql.ExprTree;

namespace KeaSql.ExprRewrite
{
    public static class DefaultRewrite
    {
        /// <summary>
        /// Reglas para simplificar expresiones booleanas
        /// </summary>
        public static readonly RewriteRule[] BooleanSimplify = new[]
        {
            RewriteRule.Create("evalEqNull",
                (RewriteTypes.C1 x) =>  x == null,
                null,
                (x, _) => (x.Args[0] is MemberExpression),
                (match, x, visit) => ExprEval.EvalExprExpr(x)),

            RewriteRule.Create("evalNEqNull",
                (RewriteTypes.C1 x) =>  x != null,
                null,
                (x, _) => (x.Args[0] is MemberExpression),
                (match, x, visit) => ExprEval.EvalExprExpr(x)),

            RewriteRule.Create("evalNotConst",
                (bool x) => !RewriteSpecial.Constant(x),
                null,
                null,
                (match, x, visit) => ExprEval.EvalExprExpr(x)
                ),

            //OR 1:
            RewriteRule.Create("x || false", (bool a) => a || false, a => a),
            RewriteRule.Create("false || x", (bool a) => false || a, a => a),

            RewriteRule.Create("x || true", (bool a) => a || true, a => true),
            RewriteRule.Create("true || x",  (bool a) => true || a, a => true),

            //OR 2:
            RewriteRule.Create("x || x", (bool a) => a || a, a => a),

            //OR 3:
            RewriteRule.Create("x || !x", (bool a) => a || !a, a => true),


            //AND 1:
            RewriteRule.Create("x && true", (bool a) => a && true, a => a),
            RewriteRule.Create("true && x", (bool a) => true && a, a => a),

            RewriteRule.Create("x && false", (bool a) => a && false, a => false),
            RewriteRule.Create("false && x", (bool a) => false && a, a => false),

            //AND 2:
            RewriteRule.Create("x && x", (bool a) => a && a, a => a),

            //AND 3:
            RewriteRule.Create("x && !x", (bool a) => a && !a, a => false),

        };


        static IReadOnlyList<(int? index, string s)> SplitStringFormat(string s)
        {
            var onIndex = false;
            var b = new StringBuilder();
            var ret = new List<(int? index, string s)>();
            foreach (var c in s)
            {
                if (c == '{')
                {
                    if (onIndex)
                        throw new ArgumentException("Caracter inválido en la cadena de String.Format");
                    onIndex = true;

                    if (b.Length > 0)
                    {
                        ret.Add((null, b.ToString()));
                        b.Clear();
                    }
                    continue;
                }
                if (c == '}')
                {
                    if (!onIndex)
                        throw new ArgumentException("Caracter inválido en la cadena de String.Format");
                    onIndex = false;
                    var indexS = b.ToString();
                    b.Clear();
                    var index = int.Parse(indexS);

                    ret.Add((index, indexS));
                    continue;
                }

                b.Append(c);
            }

            if (b.Length > 0)
            {
                ret.Add((null, b.ToString()));
            }
            return ret;
        }

        public static Expression AddStr(Expression a, Expression b)
        {
            var concat = typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) });
            return Expression.Add(a, b, concat);
        }

        /// <summary>
        /// Convierte una llamada a string.Format() a un conjunto de concatenaciones a + b + c ....
        /// </summary>
        public static readonly RewriteRule StringFormat = RewriteRule.Create(
                "stringFormat",
                () => RewriteSpecial.Call<string>(typeof(string), nameof(string.Format)),
                null,
                //Que el primer argumento sea una constante de string:
                (match, expr) => expr is MethodCallExpression call && (call.Arguments[0] is ConstantExpression) && (call.Arguments[0].Type == typeof(string)),
                (match, expr, visit) =>
                {
                    var call = expr as MethodCallExpression;
                    var strArgs = call.Arguments.Skip(1).ToList();
                    var format = (string)((ConstantExpression)call.Arguments[0]).Value;
                    var items = SplitStringFormat(format);

                    var concatArgs = items.Select(x => x.index != null ? strArgs[x.index.Value] : Expression.Constant(x.s));
                    var agg = concatArgs.Aggregate((a, b) => AddStr(a, b));

                    return agg;
                });

        [AlwaysNull]
        public static T Parameter<T>() => default(T);


        /// <summary>
        /// Expande las llamadas al Invoke
        /// </summary>
        public static RewriteRule InvokeRule(IEnumerable<RewriteRule> paramReplace) => RewriteRule.Create(
            "invoke",
            () => RewriteSpecial.Call<object>(null, "Invoke"),
            null,
            (match, expr) => expr is MethodCallExpression call && typeof(LambdaExpression).IsAssignableFrom(call.Arguments[0].Type),
            (match, expr, visit) =>
            {
                var call = expr as MethodCallExpression;
                var lambdaExprNoVisit = call.Arguments[0];
                //Quitar los parametros del lambda:

                var lambdaExpr = (paramReplace != null) ? Rewriter.RecApplyRules(lambdaExprNoVisit, paramReplace, x => false) : lambdaExprNoVisit;

                var lambda = ExprEval.EvalExpr<LambdaExpression>(lambdaExpr).Value;

                var args = call.Arguments.Skip(1).ToList();

                var body = lambda.Body;
                var replace = lambda.Parameters.Select((x, i) => (find: x, rep: args[i]));

                //Sustituir args en el body:
                var eval = ReplaceVisitor.Replace(body, replace.ToDictionary(x => (Expression)x.find, x => x.rep));

                return eval;
            });
    }
}
