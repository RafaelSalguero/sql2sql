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
        public ReplaceVisitor(Func<Expression, Expression> replace, Func<Type, Type> typeRep, Func<Expression, bool> preserve)
        {
            this.exprRep = replace;
            this.singleTypeRep = typeRep;
        }

        readonly Func<Expression, Expression> exprRep;
        readonly Func<Type, Type> singleTypeRep;
        readonly Func<Expression, bool> preserve;

        public static Expression Replace(Expression Expression, Expression Find, Expression ReplaceWith)
        {
            if (ReplaceWith == null)
                throw new ArgumentException("ReplaceWith no puede ser null");
            var V = new ReplaceVisitor(x => x == Find ? ReplaceWith : x, x => x, x => false);
            return V.Visit(Expression);
        }

        public static Expression Replace(Expression Expression, IReadOnlyDictionary<Expression, Expression> dic)
        {
            var V = new ReplaceVisitor(x => dic.TryGetValue(x, out Expression ret) ? ret : x, x => x, x => false);
            return V.Visit(Expression);
        }

        public static Expression Replace(Expression Expression, IReadOnlyDictionary<Expression, Expression> dic, IReadOnlyDictionary<Type, Type> types, Func<Expression, bool> preserve)
        {
            var V = new ReplaceVisitor(x => dic.TryGetValue(x, out var ret) ? ret : x, x => types.TryGetValue(x, out var t) ? t : x, preserve);
            return V.Visit(Expression);
        }

        public static Expression Replace(Expression Expression, Func<Expression, Expression> replace)
        {
            var V = new ReplaceVisitor(replace, x => x, x => false);
            return V.Visit(Expression);
        }

        static Type RecursiveRep(Type type, Func<Type, Type> rep)
        {
            if (!type.IsGenericType || type.IsGenericTypeDefinition)
            {
                //El tipo no es un generico concredo, sustituir sólo el primer nivel
                return rep(type);
            }

            var deff = type.GetGenericTypeDefinition();
            var args = type.GetGenericArguments();

            var deffRep = rep(deff);
            var argsRep = args.Select(x => RecursiveRep(x, rep)).ToArray();

            if (deffRep != deff || !args.SequenceEqual(argsRep))
            {
                //El tipo cambio, devolver el nuevo tipo
                return deffRep.MakeGenericType(argsRep);
            }

            //El tipo no cambio, devolver la misma instancia
            return type;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            var type = RecursiveRep(node.Type, singleTypeRep);
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
                var next = orig.Select(x => RecursiveRep(x, singleTypeRep)).ToArray();

                var change = !orig.SequenceEqual(next, CompareExpr.TypeEquals);
                if (change)
                {
                    var obj = Visit(node.Object);
                    var args = node.Arguments;
                    var newArgs = args.Select(Visit).ToArray();

                    var method = node.Method.GetGenericMethodDefinition().MakeGenericMethod(next);
                    Any = true;
                    return Expression.Call(obj, method, newArgs);
                }
            }
            return base.VisitMethodCall(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var orig = node.Expression;
            var next = Visit(orig);

            if (orig == next)
            {
                //No cambió para nada:
                return node;
            }

            var origMember = node.Member;
            if (orig.Type == next.Type)
            {
                //Cambio el member, pero no el tipo
                return Expression.MakeMemberAccess(next, origMember);
            }

            //Cambio el tipo entre el miembro original y el siguiente, por lo que se tiene que buscar el Member equivalente en el nuevo tipo:
            var nextMember = next.Type.GetMembers(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Where(x => x.MemberType == origMember.MemberType && x.Name == origMember.Name).FirstOrDefault();
            if (nextMember == null)
            {
                throw new ArgumentException($"No se pudo encontrar el miembro equivalente '{origMember}' en el tipo '{next.Type}'");
            }

            return Expression.MakeMemberAccess(next, nextMember);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var left = Visit(node.Left);
            var right = Visit(node.Right);
            var conversion = (LambdaExpression)Visit(node.Conversion);
            if (node.Left == left && node.Right == right && node.Conversion == conversion)
            {
                //No cambió para nada:
                return node;
            }

            if (node.Left.Type == left.Type && node.Right.Type == right.Type && node.Conversion?.Type == conversion?.Type)
            {
                //No cambiaron los tipos:
                return Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method, conversion);
            }

            //Si cambiaron los tipos, crea una nueva expresión dejando que el MakeBinary encuentre el MethodInfo apropiado:
            var ret = Expression.MakeBinary(node.NodeType, left, right);
            return ret;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var parPairs = node.Parameters.Select(x => (old: x, next: (ParameterExpression)Visit(x))).ToList();
            var pars = parPairs.Select(x => x.next);
            var dic = parPairs.ToDictionary(x => x.old, x => x.next);

            Func<Expression, Expression> bodyTrans = ex =>
            {
                if(ex is ParameterExpression p && dic.TryGetValue(p, out var pret))
                {
                    return pret;
                }
                return this.exprRep(ex);
            };

            var bodyVisitor = new ReplaceVisitor(bodyTrans, this.singleTypeRep, this.preserve);
            var body = bodyVisitor.Visit(node.Body);

            if (body != node.Body || !pars.SequenceEqual(node.Parameters))
            {
                var ret = Expression.Lambda(body, node.TailCall, pars);
                return ret;
            }
            return node;
        }

        /// <summary>
        /// True if any match has been found
        /// </summary>
        public bool Any = false;
        public override Expression Visit(Expression node)
        {
            if (node == null) return null;
            if (preserve?.Invoke(node) == true)
            {
                return node;
            }

            var ret = node;

            var repResult = exprRep(ret);
            if (repResult == null)
                throw new ArgumentException("La función de reemplazo no puede devolver null");
            if (repResult != ret)
            {
                ret = repResult;
                Any = true;
            }

            ret = base.Visit(ret);
            return ret;
        }
    }

}
