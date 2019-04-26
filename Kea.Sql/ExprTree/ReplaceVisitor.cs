using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace KeaSql.ExprTree
{
    /// <summary>
    /// Remplaza una expresión con otra
    /// </summary>
    public class ReplaceVisitor : ExpressionVisitor
    {
        public ReplaceVisitor(Func<Expression, Expression> replace, Func<Type, Type> typeRep)
        {
            this.exprRep = replace;
            this.typeRep = typeRep;
        }

        readonly Func<Expression, Expression> exprRep;
        readonly Func<Type, Type> typeRep;

        public static Expression Replace(Expression Expression, Expression Find, Expression ReplaceWith)
        {
            if (ReplaceWith == null)
                throw new ArgumentException("ReplaceWith no puede ser null");
            var V = new ReplaceVisitor(x => x == Find ? ReplaceWith : x, x => x);
            return V.Visit(Expression);
        }

        public static Expression Replace(Expression Expression, IReadOnlyDictionary<Expression, Expression> dic)
        {
            var V = new ReplaceVisitor(x => dic.TryGetValue(x, out Expression ret) ? ret : x, x => x);
            return V.Visit(Expression);
        }

        public static Expression Replace(Expression Expression, IReadOnlyDictionary<Expression, Expression> dic, IReadOnlyDictionary<Type, Type> types)
        {
            var V = new ReplaceVisitor(x => dic.TryGetValue(x, out var ret) ? ret : x, x => types.TryGetValue(x, out var t) ? t : x);
            return V.Visit(Expression);
        }

        public static Expression Replace(Expression Expression, Func<Expression, Expression> replace)
        {
            var V = new ReplaceVisitor(replace, x => x);
            return V.Visit(Expression);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            var type = typeRep(node.Type);
            if (type != node.Type)
            {
                node = Expression.Parameter(type, node.Name);
                Any = true;
            }
            return base.VisitParameter(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsGenericMethod)
            {
                var orig = node.Method.GetGenericArguments();
                var next = orig.Select(typeRep).ToArray();

                var change = !orig.SequenceEqual(next, CompareExpr.CompareType);
                if (change)
                {
                    var args = node.Arguments;
                    var newArgs = args.Select(Visit).ToArray();

                    var method = node.Method.GetGenericMethodDefinition().MakeGenericMethod(next);
                    Any = true;
                    return Expression.Call(node.Object, method, newArgs);
                }
            }
            return base.VisitMethodCall(node);
        }


        /// <summary>
        /// True if any match has been found
        /// </summary>
        public bool Any = false;
        public override Expression Visit(Expression node)
        {
            if (node == null) return null;
            var ret = node;

            ret = base.Visit(ret);
            var repResult = exprRep(ret);
            if (repResult == null)
                throw new ArgumentException("La función de reemplazo no puede devolver null");
            if (repResult != ret)
            {
                ret = repResult;
                Any = true;
            }

            return ret;
        }
    }

}
