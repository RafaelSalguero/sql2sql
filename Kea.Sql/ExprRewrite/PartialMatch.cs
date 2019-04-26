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
        /// <summary>
        /// Crea un PartialMatch a partir de un diccionario
        /// </summary>
        public PartialMatch(IReadOnlyDictionary<ParameterExpression, Expression> args)
        {
            Args = args;
        }

        public IReadOnlyDictionary<ParameterExpression, Expression> Args { get; }

        /// <summary>
        /// Un PartialMatch exitoso y vacio
        /// </summary>
        public static PartialMatch Empty => new PartialMatch(new Dictionary<ParameterExpression, Expression>());

        /// <summary>
        /// Un partial match de un parámetro
        /// </summary>
        public static PartialMatch FromParam(ParameterExpression param, Expression expr)
        {
            return new PartialMatch(new Dictionary<ParameterExpression, Expression>
            {
                { param, expr }
            });
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

            return new Match(ret.ToList());
        }


        /// <summary>
        /// Mezcla dos matches, devuelve null si los matches tienen parametros con valores diferentes entre sí.
        /// Si alguno de los dos es null, devuelve null
        /// </summary>
        public static PartialMatch Merge(PartialMatch a, PartialMatch b)
        {
            if (a == null || b == null)
                return null;

            var ret = new Dictionary<ParameterExpression, Expression>();
            foreach (var i in a.Args)
                ret.Add(i.Key, i.Value);

            foreach (var i in b.Args)
            {
                if (ret.TryGetValue(i.Key, out Expression ex))
                {
                    //Si el valor ya existe, comprobar que es el mismo, si no, devuelve null indicando que no hay match
                    if (!CompareExpr.ExprEquals(ex, i.Value))
                    {
                        return null;
                    }
                    //El valor ya existe
                    continue;
                }

                ret.Add(i.Key, i.Value);
            }
            return new PartialMatch(ret);
        }
    }
}
