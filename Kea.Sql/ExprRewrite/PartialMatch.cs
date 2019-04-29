using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using KeaSql.ExprTree;

namespace KeaSql.ExprRewrite
{
    /// <summary>
    /// Un match parcial en un arbol de expresion
    /// </summary>
    public class PartialMatch
    {
        public PartialMatch(IReadOnlyDictionary<Type, Type> types, IReadOnlyDictionary<ParameterExpression, Expression> args)
        {
            Types = types;
            Args = args;
        }


        /// <summary>
        /// Tipos mapeados
        /// </summary>
        public IReadOnlyDictionary<Type, Type> Types { get; }

        /// <summary>
        /// Argumentos mapeados
        /// </summary>
        public IReadOnlyDictionary<ParameterExpression, Expression> Args { get; }

        /// <summary>
        /// Un PartialMatch exitoso y vacio
        /// </summary>
        public static PartialMatch Empty => new PartialMatch(EmptyTypes, EmptyArgs);
        static readonly IReadOnlyDictionary<Type, Type> EmptyTypes = new Dictionary<Type, Type>();
        static readonly IReadOnlyDictionary<ParameterExpression, Expression> EmptyArgs = new Dictionary<ParameterExpression, Expression>();

        /// <summary>
        /// Obtiene un match de un arreglo ordenado de tipos
        /// </summary>
        public static PartialMatch FromTypes(Type[] patt, Type[] expr)
        {
            if (patt.Length != expr.Length)
                return null;
            var matches = patt.Zip(expr, (a, b) => FromType(a, b));
            return Merge(matches);
        }

        static bool IsWildcardType(Type t) => typeof(RewriteTypes.WildType).IsAssignableFrom(t);
        static bool IsMatchType(Type t) => typeof(RewriteTypes.MatchType).IsAssignableFrom(t) && t != typeof(RewriteTypes.MatchType);

        interface IArray<T> : IList<T>, IReadOnlyList<T> { }

          static bool IsAssignableToGenericType(Type givenType, Type genericType)
        {
            var interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                    return true;
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                return true;

            Type baseType = givenType.BaseType;
            if (baseType == null) return false;

            return IsAssignableToGenericType(baseType, genericType);
        }


        /// <summary>
        /// Obtiene un match de dos tipos
        /// </summary>
        public static PartialMatch FromType(Type patt, Type expr)
        {
            var origExpr = expr;
            if(patt.IsGenericType && !expr.IsGenericType && expr.IsArray)
            {
                //Considerar el tipo de arreglo como un IList
                expr = typeof(IArray<>).MakeGenericType(expr.GetElementType());
            }
            
            if (IsWildcardType(patt) || patt.IsAssignableFrom(expr))
            {
                return PartialMatch.Empty;
            }
            else if (patt.IsGenericTypeDefinition && expr.IsGenericTypeDefinition && IsAssignableToGenericType(expr,patt))
            {
                return PartialMatch.Empty;
            }
            else if (IsMatchType(patt))
            {
                return new PartialMatch(new Dictionary<Type, Type>
                {
                     { patt, expr}
                }, EmptyArgs);
            }
            else if (patt.IsGenericType && expr.IsGenericType && !patt.IsGenericTypeDefinition && !expr.IsGenericTypeDefinition)
            {
                //Si son tipos genericos concretos los dos:

                var deffMatch = FromType(patt.GetGenericTypeDefinition(), expr.GetGenericTypeDefinition());
                var argMatch = FromTypes(patt.GetGenericArguments(), expr.GetGenericArguments());
                return PartialMatch.Merge(deffMatch, argMatch);
            }

            return null;
;        }


        /// <summary>
        /// Un partial match de un parámetro, devuelve un match si el parametro tiene un tipo AnyType o si el tipo de la expresión es igual al tipo del parametro.
        /// Si el tipo del parametro no encaja devuelve null
        /// </summary>
        public static PartialMatch FromParam(ParameterExpression param, Expression expr)
        {
            var paramType = param.Type;
            var argDic = new Dictionary<ParameterExpression, Expression>
                {
                    { param, expr }
                };
            var valueMatch = new PartialMatch(EmptyTypes, argDic);
            var typeMatch = FromType(param.Type, expr.Type);

            var ret = Merge(valueMatch, typeMatch);
            return ret;
        }

        /// <summary>
        /// Mezcla un conjunto de matches, devuelve null si alguno de ellos es null o si alguno de ellos tiene parametros 
        /// con valores diferentes entre sí. Si es una colección vacía devuelve un EmptyMatch
        /// </summary>
        public static PartialMatch Merge(IEnumerable<PartialMatch> matches)
        {
            if (!matches.Any())
                return PartialMatch.Empty;

            return matches.Aggregate(Merge);
        }

        /// <summary>
        /// Convierte un PartialMatch a un Match. Devuelve null si el PartialMatch no esta completo
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public static Match ToFullMatch(PartialMatch match, IEnumerable<ParameterExpression> parameters)
        {
            if (match == null)
                return null;

            var ret = parameters.Select(x =>
            {
                if (match.Args.TryGetValue(x, out Expression value))
                {
                    return value;
                }
                return null;
            }).ToList();

            if (ret.Any(x => x == null))
                return null;

            return new Match(match.Types, ret.ToList());
        }

        static IReadOnlyDictionary<TKey, TValue> MergeDic<TKey, TValue>(IReadOnlyDictionary<TKey,TValue> a , IReadOnlyDictionary<TKey,TValue> b, Func<TValue, TValue,bool> equals)
        {
            if (a == null || b == null)
                return null;


            var ret = new Dictionary<TKey, TValue>();
            foreach (var i in a)
                ret.Add(i.Key, i.Value);

            foreach (var i in b)
            {
                if (ret.TryGetValue(i.Key, out TValue ex))
                {
                    //Si el valor ya existe, comprobar que es el mismo, si no, devuelve null indicando que no hay match
                    if (!equals(ex, i.Value))
                    {
                        return null;
                    }
                    //El valor ya existe
                    continue;
                }

                ret.Add(i.Key, i.Value);
            }

            return ret;
        }

        /// <summary>
        /// Mezcla dos matches, devuelve null si los matches tienen parametros con valores diferentes entre sí.
        /// Si alguno de los dos es null, devuelve null
        /// </summary>
        public static PartialMatch Merge(PartialMatch a, PartialMatch b)
        {
            var argDic = MergeDic(a?.Args, b?.Args, CompareExpr.ExprEquals);
            var typeDic = MergeDic(a?.Types, b?.Types, CompareExpr.TypeEquals);

            if (argDic == null || typeDic == null) return null;

            return new PartialMatch(typeDic, argDic);
        }
    }
}
