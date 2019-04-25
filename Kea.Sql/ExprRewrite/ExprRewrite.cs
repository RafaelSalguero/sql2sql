using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using KeaSql.ExprTree;

namespace KeaSql.ExprRewrite
{
    /// <summary>
    /// Funciones para aplicar reglas a expresiones
    /// </summary>
    public static class Rewriter
    {
        /// <summary>
        /// Trata de evaluar una expresión a su forma constante, si lo logra devuelve la expresión reducida, si no, devuelve null
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static Expression EvalExpr(Expression expr)
        {
            if (TryEvalExpr(expr, out object cons))
            {
                return Expression.Constant(cons);
            }
            return null;
        }

        /// <summary>
        /// Trata de evaluar una expresión y devuelve el valor de la misma
        /// </summary>
        public static bool TryEvalExpr(Expression expr, out object result)
        {
            var lambda = Expression.Lambda(expr, new ParameterExpression[0]);
            try
            {
                var comp = lambda.Compile();
                result = comp.DynamicInvoke(new object[0]);
                return true;
            }
            catch (Exception)
            {
                //No se pudo evaluar:
                result = null;
                return false;
                throw;
            }
        }

        /// <summary>
        /// Aplica recursivamente cierto conjunto de reglas a todo el arbol de expresión
        /// </summary>
        public static Expression ApplyRules(Expression expr, IEnumerable<RewriteRule> rules)
        {
            var visitor = new RewriteVisitor(rules);
            return visitor.Visit(expr);
        }

        /// <summary>
        /// Devuelve el resultado de aplicar una regla al niver superior de la expresión o null si la regla no se pudo aplicar a la expresión
        /// </summary>
        public static Expression GlobalApplyRule(Expression expr, RewriteRule rule)
        {
            var parameters = rule.Find.Parameters;
            var pattBody = rule.Find.Body;
            var partialMatch = GlobalMatch(expr, parameters, pattBody);
            var match = PartialMatch.ToFullMatch(partialMatch, parameters);

            if (match == null)
                return null;

            if (rule.Condition != null && !rule.Condition(match, expr))
                return null;

            //Sustituir la expresión:
            var ret = expr;
            if (rule.Replace != null)
            {
                var subDic = rule.Replace.Parameters
                    .Select((x, i) => (par: x, value: match.Args[i]))
                    .ToDictionary(x => (Expression)x.par, x => x.value)
                    ;

                ret = ReplaceVisitor.Replace(rule.Replace.Body, subDic);
            }

            if (rule.Transform != null)
            {
                ret = rule.Transform(match, ret);
            }

            return ret;
        }

        /// <summary>
        /// Devuelve un match en caso de que el patron encaje en la expresión o null si no encaja el patron con la expresión
        /// </summary>
        /// <param name="expr">Expresión que se quiere probar</param>
        /// <param name="parameters">Parametros a encajar</param>
        /// <param name="pattern">Cuerpo del pattern</param>
        public static PartialMatch GlobalMatch(Expression expr, IEnumerable<ParameterExpression> parameters, Expression pattern)
        {
            if (expr == null && pattern == null)
            {
                //Si los dos son null es un match exitoso:
                return PartialMatch.Empty;
            }
            else if (expr == null || pattern == null)
            {
                //Sólo uno de los dos es null
                return null;
            }
            else if (pattern is ParameterExpression pattParam && parameters.Contains(pattParam))
            {
                //Si el pattern es un parametro encaja con cualquier expresion del mismo tipo
                if (pattern.Type != expr.Type)
                    return null;

                return PartialMatch.FromParam(pattParam, expr);
            }
            else if (pattern is ConstantExpression)
            {
                //Comparar por igualdad
                var eq = CompareExpr.ExprEquals(pattern, expr);
                return eq ? PartialMatch.Empty : null;
            }
            else if (pattern is UnaryExpression pattUn)
            {
                if (!(expr is UnaryExpression exprUn))
                {
                    //Deben de ser el mismo tipo
                    return null;
                }

                if (exprUn.NodeType != pattUn.NodeType)
                    return null;

                return GlobalMatch(exprUn.Operand, parameters, pattUn.Operand);
            }
            else if (pattern is BinaryExpression pattBin)
            {
                if (!(expr is BinaryExpression exprBin))
                {
                    //Deben de ser del mismo tipo
                    return null;
                }

                //Debe de ser el mismo operador
                if (exprBin.NodeType != pattBin.NodeType)
                    return null;

                //Encajar los dos lados:
                var leftMatch = GlobalMatch(exprBin.Left, parameters, pattBin.Left);
                var rightMatch = GlobalMatch(exprBin.Right, parameters, pattBin.Right);

                return PartialMatch.Merge(leftMatch, rightMatch);
            }
            else if (pattern is MethodCallExpression pattCall)
            {
                if (pattCall.Method.DeclaringType == typeof(RewriteSpecialCalls))
                {
                    //Special call
                    switch (pattCall.Method.Name)
                    {
                        case nameof(RewriteSpecialCalls.Constant):
                            {
                                if (expr is ConstantExpression)
                                    return GlobalMatch(expr, parameters, pattCall.Arguments[0]);
                                return null;
                            }
                        case nameof(RewriteSpecialCalls.NotConstant):
                            {
                                if (expr is ConstantExpression)
                                    return null;
                                return GlobalMatch(expr, parameters, pattCall.Arguments[0]);
                            }
                        case nameof(RewriteSpecialCalls.Call):
                            {
                                //La expresión debe de ser una llamada:
                                if (!(expr is MethodCallExpression exprCall))
                                    return null;

                                var typeEx = pattCall.Arguments[0];
                                var methodNameEx = pattCall.Arguments[1];

                                var type = (Type)((ConstantExpression)typeEx)?.Value;
                                var methodName = (string)((ConstantExpression)methodNameEx).Value;


                                //El nombre del método no encaja
                                if (exprCall.Method.Name != methodName)
                                    return null;

                                //Checar que la llamada encaje:
                                if (type != null && type != exprCall.Method.DeclaringType)
                                {
                                    //El tipo no encaja
                                    return null;
                                }


                                var instance = pattCall.Arguments.Count >= 3 ? pattCall.Arguments[2] : null;
                                if (instance != null)
                                    return GlobalMatch(exprCall.Object, parameters, instance);

                                return PartialMatch.Empty;
                            }
                    }

                    throw new ArgumentException($"No se identificó el SpecialCall '{pattCall.Method.Name}'");
                }

                {
                    if (!(expr is MethodCallExpression exprCall))
                        return null;

                    if (!CompareExpr.CompareMethodInfo(exprCall.Method, pattCall.Method))
                        return null;

                    var objMatch = GlobalMatch(exprCall.Object, parameters, pattCall.Object);
                    var argMatches =
                        exprCall.Arguments
                        .Zip(pattCall.Arguments, (a, b) => (expr: a, patt: b))
                        .Select(x => GlobalMatch(x.expr, parameters, x.patt));

                    var argMatch = PartialMatch.Merge(argMatches);
                    return PartialMatch.Merge(objMatch, argMatch);
                }
            }
            else if (pattern is MemberExpression pattMem)
            {
                if (!(expr is MemberExpression exprMem))
                    return null;

                if (!CompareExpr.CompareMemberInfo(exprMem.Member, pattMem.Member))
                    return null;

                return GlobalMatch(exprMem.Expression, parameters, pattMem.Expression);
            }
            throw new ArgumentException($"No se puede hacer un match con un pattern de tipo '{pattern.GetType()}'");
        }
    }
}
