using System;
using System.Linq;
using System.Linq.Expressions;
using KeaSql.ExprRewrite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KeaSql.Test
{
    [TestClass]
    public class RewriteTest
    {
        [TestMethod]
        public void RewriteSimple()
        {
            var rule = RewriteRule.Create((bool x) => true || x, x => true);
            var a = false;

            Expression<Func<bool>> expr = () => true || a;
            var ret = Rewriter.GlobalApplyRule(expr.Body, rule);
            var str = ret.ToString();
            Assert.AreEqual(str, "True");
        }

        [TestMethod]
        public void RewriteArgs()
        {
            var hola = "Hola";
            var rafa = "Rafa";
            Expression<Func<string>> test = () => $"({hola}, {rafa})" + typeof(string).Name;
            var rule = RewriteRule.Create((bool x) => x || x, x => x);

            {
                Expression<Func<bool, bool, bool>> expr = (a, b) => a || a;
                var ret = Rewriter.GlobalApplyRule(expr.Body, rule);
                var str = ret.ToString();
                Assert.AreEqual(str, "a");
            }

            {
                Expression<Func<bool, bool, bool>> expr = (a, b) => a || b;
                var ret = Rewriter.GlobalApplyRule(expr.Body, rule);
                Assert.IsNull(ret);
            }
        }

        [TestMethod]
        public void EvalBooleanRule()
        {
            //Evalua las expresiones booleanas, sólo se aplica la regla si la expresión no es ya una constante
            var evalBool = RewriteRule.Create((bool x) => RewriteSpecialCalls.NotConstant(x), null, null, (_, x) => Rewriter.EvalExpr(x));

            var rules = new[]
            {
                evalBool
            };

            var a = true;
            Expression<Func<bool, bool, bool>> expr = (x, y) => x && y || !(true || a);
            var red = Rewriter.ApplyRules(expr, rules);
            //Note que la expresión !(true || a) se evaluó por completo:
            Assert.AreEqual("(x, y) => ((x AndAlso y) OrElse False)", red.ToString());
        }

        [TestMethod]
        public void StringFormat()
        {
            var formatToConcat = DefaultRewrite.StringFormat;

            Expression<Func<string, string, string>> test2 = (a, b) => a + b;

            Expression<Func<string, string, string>> test = (a, b) => string.Format("{0} LIKE {1}", a, b);
            var ret = Rewriter.GlobalApplyRule(test.Body, formatToConcat);
            Assert.AreEqual("((a + \" LIKE \") + b)", ret.ToString());
        }

        [TestMethod]
        public void SimplifyBoolean()
        {
            //Evalua las expresiones booleanas, sólo se aplica la regla si la expresión no es ya una constante
            var evalBool = RewriteRule.Create((bool x) => x, null, (x, _) => !(x.Args[0] is ConstantExpression), (_, x) => Rewriter.EvalExpr(x));
            var orFalse = RewriteRule.Create((bool x) => false || x, x => x);
            var orTrue = RewriteRule.Create((bool x) => true || x, x => true);

            var andTrue = RewriteRule.Create((bool x) => x && true, x => x);
            var andFalse = RewriteRule.Create((bool x) => x && false, x => false);

            var rules = new[]
            {
                evalBool,
                orFalse,
                orTrue,
                andTrue,
                andFalse
            };

            int? id = null;
            Expression<Func<int?, bool>> expr = x =>
                    (id == null) || id == x;

            {
                var red = Rewriter.ApplyRules(expr, rules);
                //Note que la expresión !(true || a) se evaluó por completo:
                Assert.AreEqual("x => True", red.ToString());
            }

            {
                id = 10;
                var red = Rewriter.ApplyRules(expr, rules);
                Expression<Func<int?, bool>> expected = x => id == x;
                //Note que la expresión !(true || a) se evaluó por completo:
                Assert.AreEqual(expected.ToString(), red.ToString());
            }
        }

    }
}
