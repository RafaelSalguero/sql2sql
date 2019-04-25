using System.Collections.Generic;
using System.Linq.Expressions;

namespace KeaSql.ExprRewrite
{
    /// <summary>
    /// Una unificación del patron de búsqueda de un <see cref="RewriteRule"/>
    /// </summary>
    public class Match
    {
        public Match(IReadOnlyList<Expression> args)
        {
            Args = args;
        }

        /// <summary>
        /// Valores que se asignaron a los argumentos
        /// </summary>
        public IReadOnlyList<Expression> Args { get; }
    }
}
