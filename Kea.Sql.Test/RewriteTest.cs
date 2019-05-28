using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using KeaSql.ExprRewrite;
using KeaSql.SqlText;
using KeaSql.SqlText.Rewrite;
using KeaSql.SqlText.Rewrite.Rules;
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
            var ret = Rewriter.RecApplyRules(expr, rules, SqlFunctions.ExcludeFromRewrite);
            return ret;
        }

        [TestMethod]
        public void RewriteSimple()
        {
            var rule = RewriteRule.Create("", (bool x) => true || x, x => true);
            var a = false;

            Expression<Func<bool>> expr = () => true || a;
            var ret = Rewriter.GlobalApplyRule(expr.Body, rule, x => x);
            var str = ret.ToString();
            Assert.AreEqual(str, "True");
        }

        [TestMethod]
        public void RewriteArgs()
        {
            var hola = "Hola";
            var rafa = "Rafa";
            Expression<Func<string>> test = () => $"({hola}, {rafa})" + typeof(string).Name;
            var rule = RewriteRule.Create("", (bool x) => x || x, x => x);

            {
                Expression<Func<bool, bool, bool>> expr = (a, b) => a || a;
                var ret = Rewriter.GlobalApplyRule(expr.Body, rule, x => x);
                var str = ret.ToString();
                Assert.AreEqual(str, "a");
            }

            {
                Expression<Func<bool, bool, bool>> expr = (a, b) => a || b;
                var ret = Rewriter.GlobalApplyRule(expr.Body, rule, x => x);
                Assert.AreEqual(ret, expr.Body);
            }
        }

        [TestMethod]
        public void EvalBooleanRule()
        {
            //Evalua las expresiones booleanas, sólo se aplica la regla si la expresión no es ya una constante
            var evalBool = RewriteRule.Create("", (bool x) => RewriteSpecial.NotConstant(x), null, null, (_, x, visit) => ExprEval.EvalExprExpr(x));

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
            var rules = DefaultRewrite.BooleanSimplify;

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
                SqlFunctions.rawAtom.Concat(
                    SqlFunctions.ExprParamsRules(pars)
                )
                .Concat(new[] {
                    DefaultRewrite.StringFormat
                })
                .Concat(SqlFunctions.stringCalls)
                .Concat(SqlFunctions.AtomRawRule(pars))
                .ToList();


            var ret = ApplyRules(selectBody, rules);
            var rawBody = ((MethodCallExpression)((LambdaExpression)ret).Body).Arguments[0];
            ExprEval.TryEvalExpr<string>(rawBody, out var rawStr);
            var expected = "(cli.\"Nombre\" LIKE '%' || greatest('Rafa', 'Hola') || '%')";
            Assert.AreEqual(expected, rawStr);
        }

        [TestMethod]
        public void ToSqlStringConcat()
        {
            Expression<Func<Uruz.Factura, string>> nombreFactura = x => x.Serie + "-" + x.Folio;
            Expression<Func<Uruz.Factura, string>> selectBody = x => nombreFactura.Invoke(x);

            var pars = new SqlExprParams(selectBody.Parameters[0], null, false, "cli", new SqlFromList.ExprStrAlias[0], ParamMode.None, new SqlParamDic());



            var visitor = new SqlRewriteVisitor(pars);
            var ret = visitor.Visit(selectBody);
            var rawBody = ((MethodCallExpression)((LambdaExpression)ret).Body).Arguments[0];
            ExprEval.TryEvalExpr<string>(rawBody, out var rawStr);
            var expected = @"((cli.""Serie"" || '-') || cli.""Folio"")";
            Assert.AreEqual(expected, rawStr);
        }


        [TestMethod]
        public void ToSqlRule2()
        {
            Expression<Func<Cliente, string>> selectBody = x => x.Nombre.ToLower();

            var pars = new SqlExprParams(selectBody.Parameters[0], null, false, "cli", new SqlFromList.ExprStrAlias[0], ParamMode.None, new SqlParamDic());

            var rules =
                SqlFunctions.rawAtom.Concat(
                    SqlFunctions.ExprParamsRules(pars)
                )
                .Concat(new[] {
                    DefaultRewrite.StringFormat,
                    SqlFunctions.rawCallRule
                })
                .Concat(SqlFunctions.stringCalls)
                .Concat(SqlFunctions.AtomRawRule(pars))
                .ToList();


            var ret = ApplyRules(selectBody, rules);
            var rawBody = ((MethodCallExpression)((LambdaExpression)ret).Body).Arguments[0];
            ExprEval.TryEvalExpr<string>(rawBody, out var rawStr);
            var expected = "lower(cli.\"Nombre\")";
            Assert.AreEqual(expected, rawStr);
        }

        [TestMethod]
        public void ToSqlRule3()
        {
            Expression<Func<Cliente, bool>> selectBody = x => x.Nombre.Contains("Hola");

            var pars = new SqlExprParams(selectBody.Parameters[0], null, false, "cli", new SqlFromList.ExprStrAlias[0], ParamMode.None, new SqlParamDic());

            var rules =
               SqlFunctions.rawAtom.Concat(
                   SqlFunctions.ExprParamsRules(pars)
               )
               .Concat(new[] {
                    DefaultRewrite.StringFormat
               })
               .Concat(SqlFunctions.stringCalls)
               .Concat(SqlFunctions.AtomRawRule(pars))
               .ToList();

            var ret = ApplyRules(selectBody, rules);
            var rawBody = ((MethodCallExpression)((LambdaExpression)ret).Body).Arguments[0];
            ExprEval.TryEvalExpr<string>(rawBody, out var rawStr);
            var expected = "(cli.\"Nombre\" LIKE '%' || 'Hola' || '%')";
            Assert.AreEqual(expected, rawStr);
        }

        [TestMethod]
        public void InvokeRule()
        {
            Expression<Func<int, int>> sumar = x => (x + 10);
            Expression<Func<int, int>> test = y => sumar.Invoke(y * 3);

            var rule = DefaultRewrite.InvokeRule(null);
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
                DefaultRewrite.InvokeRule(null)
        };

            var ret = ApplyRules(test, rules);
            var expected = "y => ((y * 3) + 10)";
            Assert.AreEqual(expected, ret.ToString());
        }

        [TestMethod]
        public void GenericTypeTest()
        {
            var patt = typeof(ISqlSelect<RewriteTypes.C1>);
            var expr = typeof(Fluent.ISqlGroupByAble<Cliente, Factura, object>);

            var ret = PartialMatch.FromType(patt, expr);
            Assert.AreEqual(1, ret.Types.Count);
            Assert.AreEqual(typeof(Factura), ret.Types[typeof(RewriteTypes.C1)]);
        }

        [TestMethod]
        public void BetweenTest()
        {
            Expression<Func<int, bool>> test = y => Sql.Between(y, 10, 20);

            var rules = new[] { SqlFunctions.betweenRule };

            var ret = ApplyRules(test, rules);
            var expected = "y => Raw(Format(\"{0} BETWEEN {1} {2}\", ToSql(y), ToSql(10), ToSql(20)))";
            Assert.AreEqual(expected, ret.ToString());
        }

        [TestMethod]
        public void BinaryOpTest()
        {
            Expression<Func<bool, bool, bool>> test = (a, b) => a && b;

            var rules = SqlOperators.binaryRules;

            var ret = ApplyRules(test, rules);
            var expected = "(a, b) => Raw(Format(\"({0} {1} {2})\", ToSql(a), SqlOperators.opNames.get_Item(AndAlso), ToSql(b)))";
            Assert.AreEqual(expected, ret.ToString());
        }

        [TestMethod]
        public void MultipleIfCond()
        {
            var filtro = new Uruz.FiltroFacturas
            {
                FechaInicio = new DateTime(2019,01,26)
            };

            Expression<Func<Uruz.FacturaDTO, bool>> expr = x =>
                SqlExpr.IfCond.Invoke(filtro.FechaInicio != null, x.FechaCreacion >= filtro.FechaInicio) &&
                SqlExpr.IfCond.Invoke(filtro.FechaFinal != null, x.FechaCreacion <= filtro.FechaFinal) &&
                SqlExpr.IfCond.Invoke(filtro.FechaPagoInicio != null, x.FechaPago >= filtro.FechaPagoInicio) &&
                SqlExpr.IfCond.Invoke(filtro.FechaPagoFinal != null, x.FechaPago <= filtro.FechaPagoFinal)
                ;

            var pars = new SqlExprParams(expr.Parameters[0], null, false, "fac", new SqlFromList.ExprStrAlias[0], ParamMode.EntityFramework, new SqlParamDic());

            var visitor = new SqlRewriteVisitor(pars);
            var ret = visitor.Visit(expr);
            var apps = RewriteVisitor.applications;

            var expected = "x => Raw(\"(fac.\"FechaCreacion\" >= @FechaInicio)\")";
            var actual = ret.ToString();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MultipleIfCond2()
        {
            var filtro = new Uruz.FiltroFacturas
            {
                IdCliente = 10,
            };

            Expression<Func<Uruz.FacturaDTO, bool>> expr = x =>
                SqlExpr.EqualsNullable.Invoke(x.IdCliente, filtro.IdCliente) &&
                SqlExpr.EqualsNullable.Invoke(x.IdSucursal, filtro.IdSucursal)
                ;

            var pars = new SqlExprParams(expr.Parameters[0], null, false, "fac", new SqlFromList.ExprStrAlias[0], ParamMode.EntityFramework, new SqlParamDic());

            var visitor = new SqlRewriteVisitor(pars);
            var ret = visitor.Visit(expr);
            var apps = RewriteVisitor.applications;

            var expected = "x => Raw(\"(fac.\"IdCliente\" = @IdCliente)\")";
            var actual = ret.ToString();
            Assert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void BinaryOpStrTest()
        {
            Expression<Func<Cliente, string>> select = (cli) => cli.Nombre + cli.Nombre;
            var pars = new SqlExprParams(select.Parameters[0], null, false, "cli", new SqlFromList.ExprStrAlias[0], ParamMode.None, new SqlParamDic());


            var rules = SqlFunctions.rawAtom.Concat(
                SqlOperators.binaryRules
                )
                .Concat(
                    SqlFunctions.AtomRawRule(pars)
                )
                ;

            var ret = (LambdaExpression)ApplyRules(select, rules);
            var raw = ExprEval.EvalExpr<string>(((MethodCallExpression)ret.Body).Arguments[0]).Value;
            var expected = "(cli.\"Nombre\" || cli.\"Nombre\")";
            Assert.AreEqual(expected, raw);
        }

        [TestMethod]
        public void BinaryOpIntStrTest()
        {
            Expression<Func<int, int, int>> test = (a, b) => a + b;

            var rules = SqlOperators.binaryRules;

            var ret = ApplyRules(test, rules);
            var expected = "(a, b) => Raw(Format(\"({0} {1} {2})\", ToSql(a), SqlOperators.opNames.get_Item(Add), ToSql(b)))";
            Assert.AreEqual(expected, ret.ToString());
        }

        [TestMethod]
        public void NullableValueTest()
        {
            Expression<Func<int?, int>> test = x => x.Value;

            var rules = SqlOperators.nullableRules;

            var ret = ApplyRules(test, rules);
            var expected = "x => Raw(ToSql(x))";
            Assert.AreEqual(expected, ret.ToString());
        }
        [TestMethod]
        public void NullableHasValueTest()
        {
            Expression<Func<int?, bool>> test = x => x.HasValue;

            var rules = SqlOperators.nullableRules;

            var ret = ApplyRules(test, rules);
            Assert.AreEqual(((LambdaExpression)ret).Parameters[0], test.Parameters[0]);
            Assert.AreEqual(((LambdaExpression)ret).Parameters[0], ((BinaryExpression)((LambdaExpression)ret).Body).Left);

            var expected = "x => (x != null)";
            Assert.AreEqual(expected, ret.ToString());
        }

        [TestMethod]
        public void ToSqlRuleContains()
        {
            var nombres = new[] { "rafa", "hola" };
            Expression<Func<Cliente, string[]>> selectBody = x => Sql.Record(nombres);

            var pars = new SqlExprParams(selectBody.Parameters[0], null, false, "cli", new SqlFromList.ExprStrAlias[0], ParamMode.None, new SqlParamDic());

            var rules =
               SqlFunctions.AtomRawRule(pars)
               .Concat(
                   SqlFunctions.ExprParamsRules(pars)
               )
               .Concat(new[] {
                    DefaultRewrite.StringFormat
               })
               .Concat(SqlFunctions.stringCalls)
               .Concat(SqlFunctions.sqlCalls)
               .Concat(SqlFunctions.rawAtom)
               .ToList();

            var ret = ApplyRules(selectBody, rules);
            var rawBody = ((MethodCallExpression)((LambdaExpression)ret).Body).Arguments[0];
            ExprEval.TryEvalExpr<string>(rawBody, out var rawStr);
            var expected = "('rafa', 'hola')";
            Assert.AreEqual(expected, rawStr);
        }



        [TestMethod]
        public void ToSqlRuleContainsifCond()
        {
            string[] nombres = null;


            Expression<Func<Cliente, bool>> selectBody = x => SqlExpr.IfCond.Invoke(nombres.Any(),  nombres.Contains(x.Nombre));
            var pars = new SqlExprParams(selectBody.Parameters[0], null, false, "cli", new SqlFromList.ExprStrAlias[0], ParamMode.None, new SqlParamDic());

            var visitor = new SqlRewriteVisitor(pars);

            {
                nombres   = new[] { "rafa", "hola" };
                var ret = visitor.Visit(selectBody);
                var rawBody = ((MethodCallExpression)((LambdaExpression)ret).Body).Arguments[0];
                ExprEval.TryEvalExpr<string>(rawBody, out var rawStr);
                var expected = "(cli.\"Nombre\" IN ('rafa', 'hola'))";
                Assert.AreEqual(expected, rawStr);
            }

            {
                nombres = new string[0];

                var ret = visitor.Visit(selectBody);
                var rawBody = ((MethodCallExpression)((LambdaExpression)ret).Body).Arguments[0];
                ExprEval.TryEvalExpr<string>(rawBody, out var rawStr);
                var expected = "True";
                Assert.AreEqual(expected, rawStr);
            }
        }

        [TestMethod]
        public void ToSqlRuleContainsEmpty()
        {
            var nombres = new string[0];

            Expression<Func<Cliente, bool>> selectBody = x =>nombres.Contains(x.Nombre);
            var pars = new SqlExprParams(selectBody.Parameters[0], null, false, "cli", new SqlFromList.ExprStrAlias[0], ParamMode.None, new SqlParamDic());

            var visitor = new SqlRewriteVisitor(pars);

            {
                nombres = new string[0];

                var ret = visitor.Visit(selectBody);
                var rawBody = ((MethodCallExpression)((LambdaExpression)ret).Body).Arguments[0];
                ExprEval.TryEvalExpr<string>(rawBody, out var rawStr);
                var expected = "False";
                Assert.AreEqual(expected, rawStr);
            }
        }
    }
}
