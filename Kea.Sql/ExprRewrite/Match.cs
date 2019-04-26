using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace KeaSql.ExprRewrite
{
    /// <summary>
    /// Una unificación del patron de búsqueda de un <see cref="RewriteRule"/>
    /// </summary>
    public class Match
    {
        public Match(IReadOnlyDictionary<Type, Type> types, IReadOnlyList<Expression> args)
        {
            Types = types;
            Args = args;
        }

        /// <summary>
        /// Tipos encajados
        /// </summary>
        public IReadOnlyDictionary<Type, Type> Types { get; }

        /// <summary>
        /// Valores que se asignaron a los argumentos
        /// </summary>
        public IReadOnlyList<Expression> Args { get; }
    }
}
