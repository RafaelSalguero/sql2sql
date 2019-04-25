using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace KeaSql.ExprRewrite
{
    public static class DefaultRewrite
    {
        /// <summary>
        /// Reglas para simplificar expresiones booleanas
        /// </summary>
        public static readonly RewriteRule[] BooleanSimplify = new[]
        {
            //EVAL:
            RewriteRule.Create((bool x) =>  x, null, (x, _) => !(x.Args[0] is ConstantExpression), (_,x) => Rewriter.EvalExpr(x)),

            //OR 1:
            RewriteRule.Create((bool a) => a || false, a => a),
            RewriteRule.Create((bool a) => false || a, a => a),

            RewriteRule.Create((bool a) => a || true, a => true),
            RewriteRule.Create((bool a) => true || a, a => true),

            //OR 2:
            RewriteRule.Create((bool a) => a || a, a => a),

            //OR 3:
            RewriteRule.Create((bool a) => a || !a, a => true),


            //AND 1:
            RewriteRule.Create((bool a) => a && true, a => a),
            RewriteRule.Create((bool a) => true && a, a => a),

            RewriteRule.Create((bool a) => a && false, a => false),
            RewriteRule.Create((bool a) => false && a, a => false),

            //AND 2:
            RewriteRule.Create((bool a) => a && a, a => a),

            //AND 3:
            RewriteRule.Create((bool a) => a && !a, a => false),

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

        /// <summary>
        /// Convierte una llamada a string.Format() a un conjunto de concatenaciones a + b + c ....
        /// </summary>
        public static readonly RewriteRule StringFormat = RewriteRule.Create(
                () => RewriteSpecialCalls.Call<string>(typeof(string), nameof(string.Format)),
                null,
                //Que el primer argumento sea una constante de string:
                (match, expr) => expr is MethodCallExpression call && (call.Arguments[0] is ConstantExpression) && (call.Arguments[0].Type == typeof(string)),
                (match, expr) =>
                {
                    var call = expr as MethodCallExpression;
                    var strArgs = call.Arguments.Skip(1).ToList();
                    var format = (string)((ConstantExpression)call.Arguments[0]).Value;
                    var items = SplitStringFormat(format);

                    var concatArgs = items.Select(x => x.index != null ? strArgs[x.index.Value] : Expression.Constant(x.s));
                    var concat = typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) });
                    var agg = concatArgs.Aggregate((a, b) => Expression.Add(a, b, concat));

                    return agg;
                });
    }
}
