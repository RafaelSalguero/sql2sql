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
        /// Trata de evaluar una expresión a su forma constante, si lo logra devuelve la expresión reducida, si no, devuelve la expresión original
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static Expression EvalExprExpr(Expression expr)
        {
            if (TryEvalExpr(expr, out object cons))
            {
                return Expression.Constant(cons);
            }
            return expr;
        }

        public static Expression RecApplyRules(Expression expr, IEnumerable<RewriteRule> rules, Func<Expression, bool> exclude)
        {
            var rewriter = new RewriteVisitor(rules, exclude);
            var ret = rewriter.Visit(expr);

            //Quitar los atoms:
            var r2 = new RewriteVisitor(new[]
            {
                RewriteRule.Create(
                    (RewriteTypes.C1 x) => RewriteSpecial.Atom(x),
                    x =>  x)
            }, x => false);

            var sinAtoms = r2.Visit(ret);

            return sinAtoms;
        }

        /// <summary>
        /// Evalua una expresión
        /// </summary>
        public static T EvalExpr<T>(Expression expr)
        {
            if (TryEvalExpr(expr, out T cons))
            {
                return cons;
            }
            throw new ArgumentException($"No se pudo evaluar la expresión '{expr}'");
        }

        /// <summary>
        /// Trata de evaluar una expresión y devuelve el valor de la misma
        /// </summary>
        public static bool TryEvalExpr<T>(Expression expr, out T result)
        {
            var lambda = Expression.Lambda(expr, new ParameterExpression[0]);
            try
            {
                var comp = lambda.Compile();
                result = (T)comp.DynamicInvoke(new object[0]);
                return true;
            }
            catch (Exception ex)
            {
                //No se pudo evaluar:
                result = default(T);
                return false;
                throw;
            }
        }

        /// <summary>
        /// Devuelve el resultado de aplicar una regla al niver superior de la expresión o la expresión original si la regla no se pudo aplicar a la expresión
        /// </summary>
        public static Expression GlobalApplyRule(Expression expr, RewriteRule rule, Func<Expression, Expression> visit)
        {
            var parameters = rule.Find.Parameters;
            var pattBody = rule.Find.Body;
            var partialMatch = GlobalMatch(expr, parameters, pattBody);
            var match = PartialMatch.ToFullMatch(partialMatch, parameters);

            if (match == null)
                return expr;

            if (rule.Condition != null && !rule.Condition(match, expr))
                return expr;

            //Sustituir la expresión:
            var ret = expr;
            var replaceLambda = rule.Replace;
            if (replaceLambda != null)
            {
                var subDic = replaceLambda.Parameters
                    .Select((x, i) => (par: x, value: match.Args[i]))
                    .ToDictionary(x => (Expression)x.par, x => x.value)
                    ;

                var repRet = ReplaceVisitor.Replace(replaceLambda.Body, subDic, match.Types, x => false);

                ret = repRet;
            }

            //Aplicar los transforms:
            {
                var nextRet = ReplaceVisitor.Replace(ret, ex =>
                {
                    if (ex is MethodCallExpression call && call.Method.DeclaringType == typeof(RewriteSpecial) && call.Method.Name == nameof(RewriteSpecial.Transform))
                    {
                        //Aplica el transform a la expresión:
                        var arg = call.Arguments[0];
                        var func = EvalExpr<Func<Expression, Expression>>(call.Arguments[1]);

                        var tResult = func(arg);
                        return tResult;
                    }
                    return ex;
                });

                if (nextRet != ret)
                {
                    ret = nextRet;
                }
            }

            if (rule.Transform != null)
            {
                var transRet = rule.Transform(match, ret, visit);
                if (transRet == null)
                    throw new ArgumentException("La función de transformación no debe de devolver null");
                ret = transRet;
            }

            if(ret == null)
            {
                ;
            }
            return ret;
        }

        public static PartialMatch GlobalMatch(Expression expr, LambdaExpression lambda)
        {
            return GlobalMatch(expr, lambda.Parameters, lambda.Body);
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
                return PartialMatch.FromParam(pattParam, expr);
            }
            else if (
                //Tipos que se van a comparar por igualdad:
                pattern is ConstantExpression ||
                pattern is ParameterExpression
                )
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
            else if (
                pattern is MethodCallExpression spCall &&
                spCall.Method.DeclaringType == typeof(RewriteSpecial) &&
                //Note que el atom se trata como un patron normal en el global match
                spCall.Method.Name != nameof(RewriteSpecial.Atom))
            {
                //Special call
                switch (spCall.Method.Name)
                {
                    case nameof(RewriteSpecial.Constant):
                        {
                            if (expr is ConstantExpression)
                                return GlobalMatch(expr, parameters, spCall.Arguments[0]);
                            return null;
                        }
                    case nameof(RewriteSpecial.NotConstant):
                        {
                            if (expr is ConstantExpression)
                                return null;
                            return GlobalMatch(expr, parameters, spCall.Arguments[0]);
                        }
                    case nameof(RewriteSpecial.Call):
                        {
                            //La expresión debe de ser una llamada:
                            if (!(expr is MethodCallExpression exprCall))
                                return null;

                            var typeEx = spCall.Arguments[0];
                            var methodNameEx = spCall.Arguments[1];

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


                            var instance = spCall.Arguments.Count >= 3 ? spCall.Arguments[2] : null;
                            if (instance != null)
                                return GlobalMatch(exprCall.Object, parameters, instance);

                            return PartialMatch.Empty;
                        }
                    case nameof(RewriteSpecial.Operator):
                        {

                            //Si son 3 parametros es operador binario, si no, es unario
                            var binary = spCall.Arguments.Count == 3;

                            var argType = spCall.Arguments.Last();
                            var exprTypeMatch = GlobalMatch(Expression.Constant(expr.NodeType, argType.Type), parameters, argType );

                            var generics = spCall.Method.GetGenericArguments();
                            var retType = generics.Last();
                            var retTypeMatch = PartialMatch.FromType(retType, expr.Type);
                            if (binary && expr is BinaryExpression binExpr)
                            {
                                var leftTypeMatch = PartialMatch.FromType(generics[0], binExpr.Left.Type);
                                var rightTypeMatch = PartialMatch.FromType(generics[1], binExpr.Right.Type);

                                var leftArgMatch = GlobalMatch(binExpr.Left, parameters, spCall.Arguments[0]);
                                var rightArgMatch = GlobalMatch(binExpr.Right, parameters, spCall.Arguments[1]);

                                var match = PartialMatch.Merge(new[] {
                                    exprTypeMatch,
                                    retTypeMatch, leftTypeMatch, rightTypeMatch,
                                    leftArgMatch, rightArgMatch
                                    });
                                return match;
                            }
                            else if (!binary && expr is UnaryExpression unExpr)
                            {
                                var opTypeMatch = PartialMatch.FromType(generics[0], unExpr.Operand.Type);

                                var argMatch = GlobalMatch(unExpr.Operand, parameters, spCall.Arguments[0]);

                                var match = PartialMatch.Merge(new[] {
                                    exprTypeMatch,
                                    retTypeMatch, opTypeMatch,
                                    argMatch
                                    });

                                return match;
                            }

                            return null;
                        }
                }

                throw new ArgumentException($"No se identificó el SpecialCall '{spCall.Method.Name}'");
            }
            else if (pattern is MethodCallExpression pattCall)
            {
                if (!(expr is MethodCallExpression exprCall))
                    return null;

                var callGeneric = pattCall.Method.IsGenericMethod;
                //Si el patron es generic, la expresión debe de ser generic
                if (callGeneric != exprCall.Method.IsGenericMethod)
                    return null;

                var callMethod = callGeneric ? exprCall.Method.GetGenericMethodDefinition() : exprCall.Method;
                var pattMethod = callGeneric ? pattCall.Method.GetGenericMethodDefinition() : pattCall.Method;

                if (!CompareExpr.CompareMethodInfo(callMethod, pattMethod))
                    return null;

                //Sacar los tipos:
                var typeMatch = callGeneric ? PartialMatch.FromTypes(pattCall.Method.GetGenericArguments(), exprCall.Method.GetGenericArguments()) : PartialMatch.Empty;

                var objMatch = GlobalMatch(exprCall.Object, parameters, pattCall.Object);
                var argMatches =
                    exprCall.Arguments
                    .Zip(pattCall.Arguments, (a, b) => (expr: a, patt: b))
                    .Select(x => GlobalMatch(x.expr, parameters, x.patt))
                    .ToList()
                    ;

                var argMatch = PartialMatch.Merge(argMatches);
                return PartialMatch.Merge(new[] { typeMatch, objMatch, argMatch });
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
