﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using KeaSql.ExprRewrite;
using KeaSql.SqlText;
using KeaSql.SqlText.Rewrite;
using KeaSql.Tests;
using LinqKit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KeaSql.Test
{
    [TestClass]
    public class RewriteTest
    {
        /// <summary>
        /// Aplica recursivamente cierto conjunto de reglas a todo el arbol de expresión
        /// </summary>
          static Expression ApplyRules(Expression expr, IEnumerable<RewriteRule> rules)
        {
            var visitor = new RewriteVisitor(rules, ex => false);
            return visitor.Visit(expr);
        }

        [TestMethod]
        public void RewriteSimple()
        {
            var rule = RewriteRule.Create((bool x) => true || x, x => true);
            var a = false;

            Expression<Func<bool>> expr = () => true || a;
            var ret = Rewriter.GlobalApplyRule(expr.Body, rule, x=> x);
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
                var ret = Rewriter.GlobalApplyRule(expr.Body, rule, x => x);
                var str = ret.ToString();
                Assert.AreEqual(str, "a");
            }

            {
                Expression<Func<bool, bool, bool>> expr = (a, b) => a || b;
                var ret = Rewriter.GlobalApplyRule(expr.Body, rule, x => x);
                Assert.IsNull(ret);
            }
        }

        [TestMethod]
        public void EvalBooleanRule()
        {
            //Evalua las expresiones booleanas, sólo se aplica la regla si la expresión no es ya una constante
            var evalBool = RewriteRule.Create((bool x) => RewriteSpecialCalls.NotConstant(x), null, null, (_, x, visit) => Rewriter.EvalExpr(x));

            var rules = new[]
            {
                evalBool
            };

            var a = true;
            Expression<Func<bool, bool, bool>> expr = (x, y) => x && y || !(true || a);
            var red = ApplyRules(expr, rules);
            //Note que la expresión !(true || a) se evaluó por completo:
            Assert.AreEqual("(x, y) => ((x AndAlso y) OrElse False)", red.ToString());
        }

        [TestMethod]
        public void StringFormat()
        {
            var formatToConcat = DefaultRewrite.StringFormat;

            Expression<Func<string, string, string>> test2 = (a, b) => a + b;

            Expression<Func<string, string, string>> test = (a, b) => string.Format("{0} LIKE {1}", a, b);
            var ret = Rewriter.GlobalApplyRule(test.Body, formatToConcat, x => x);
            Assert.AreEqual("((a + \" LIKE \") + b)", ret.ToString());
        }

        [TestMethod]
        public void SimplifyBoolean()
        {
            //Evalua las expresiones booleanas, sólo se aplica la regla si la expresión no es ya una constante
            var evalBool = RewriteRule.Create((bool x) => x, null, (x, _) => !(x.Args[0] is ConstantExpression), (_, x, visit) => Rewriter.EvalExpr(x));
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
                var red = ApplyRules(expr, rules);
                //Note que la expresión !(true || a) se evaluó por completo:
                Assert.AreEqual("x => True", red.ToString());
            }

            {
                id = 10;
                var red = ApplyRules(expr, rules);
                Expression<Func<int?, bool>> expected = x => id == x;
                //Note que la expresión !(true || a) se evaluó por completo:
                Assert.AreEqual(expected.ToString(), red.ToString());
            }
        }

        [TestMethod]
        public void ToSqlRule()
        {
            Expression<Func<Cliente, bool>> selectBody = x => x.Nombre.Contains(Sql.Greatest("Rafa", "Hola"));

            var pars = new SqlExprParams(selectBody.Parameters[0], null, false, "cli", new SqlFromList.ExprStrAlias[0], ParamMode.None, new SqlParamDic());

            var rules =
                SqlRewriteRules.ExprParamsRules(pars)
                .Concat(new[] {
                    DefaultRewrite.StringFormat
                })
                .Concat(SqlRewriteRules.stringCalls)
                .ToList();


            var ret = ApplyRules(selectBody, rules);
            var rawBody = ((MethodCallExpression)((LambdaExpression)ret).Body).Arguments[0];
            Rewriter.TryEvalExpr(rawBody, out var rawStr);
            var expected = "(cli.\"Nombre\" LIKE '%' || greatest('Rafa', 'Hola') || '%')";
            Assert.AreEqual(expected, rawStr);
        }

        [TestMethod]
        public void ToSqlRule2()
        {
            Expression<Func<Cliente, string>> selectBody = x => x.Nombre.ToLower();

            var pars = new SqlExprParams(selectBody.Parameters[0], null, false, "cli", new SqlFromList.ExprStrAlias[0], ParamMode.None, new SqlParamDic());

            var rules =
                SqlRewriteRules.stringCalls
                .Concat(SqlRewriteRules.ExprParamsRules(pars))
                .Concat(new[] {
                    DefaultRewrite.StringFormat,
                    SqlRewriteRules.rawCallRule
                })
                
                .ToList();


            var ret = ApplyRules(selectBody, rules);
            var rawBody = ((MethodCallExpression)((LambdaExpression)ret).Body).Arguments[0];
            Rewriter.TryEvalExpr(rawBody, out var rawStr);
            var expected = "lower(cli.\"Nombre\")";
            Assert.AreEqual(expected, rawStr);
        }

        [TestMethod]
        public void ToSqlRule3()
        {
            Expression<Func<Cliente, bool>> selectBody = x => x.Nombre.Contains("Hola");

            var pars = new SqlExprParams(selectBody.Parameters[0], null, false, "cli", new SqlFromList.ExprStrAlias[0], ParamMode.None, new SqlParamDic());

            var rules =
                SqlRewriteRules.ExprParamsRules(pars)
                .Concat(new[] {
                    DefaultRewrite.StringFormat
                })
                .Concat(SqlRewriteRules.stringCalls)
                .ToList();


            var ret = ApplyRules(selectBody, rules);
            var rawBody = ((MethodCallExpression)((LambdaExpression)ret).Body).Arguments[0];
            Rewriter.TryEvalExpr(rawBody, out var rawStr);
            var expected = "(cli.\"Nombre\" LIKE '%' || 'Hola' || '%')";
            Assert.AreEqual(expected, rawStr);
        }

        [TestMethod]
        public void InvokeRule()
        {
            Expression<Func<int, int>> sumar = x => (x + 10);
            Expression<Func<int, int>> test = y => sumar.Invoke(y * 3);

            var rule = DefaultRewrite.InvokeRule;
            var ret = Rewriter.GlobalApplyRule(test.Body, rule, x => x);
            var expected = "((y * 3) + 10)";
            Assert.AreEqual(expected, ret.ToString());
        }

        [TestMethod]
        public void InvokeRecRule()
        {
            Expression<Func<int, int, int>> sumar = (a, b) => a + b;
            Expression<Func<int, int>> sumar10 = x => sumar.Invoke(x, 10);
            Expression<Func<int, int>> test = y => sumar10.Invoke(y * 3);

            var rules = new[] {
                DefaultRewrite.InvokeRule
        };

            var ret = ApplyRules(test, rules);
            var expected = "y => ((y * 3) + 10)";
            Assert.AreEqual(expected, ret.ToString());
        }
    }
}