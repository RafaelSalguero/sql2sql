using System;
using System.Linq.Expressions;

namespace KeaSql.ExprRewrite
{
    public delegate Expression TransformDelegate(Match match, Expression expr, Func<Expression, Expression> visit);
    /// <summary>
    /// Una regla que reescribe parte de un arbol de expresión
    /// </summary>
    public class RewriteRule
    {
        public RewriteRule(LambdaExpression find, LambdaExpression replace, Func<Match,Expression, bool> condition, TransformDelegate transform)
        {
            Find = find;
            Replace = replace;
            Condition = condition;
            Transform = transform;
        }

        public static RewriteRule Create<TResult>(Expression<Func<TResult>> find, Expression<Func<TResult>> replace = null, Func<Match, Expression, bool> condition = null, TransformDelegate transform = null) => new RewriteRule(find, replace, condition, transform);
        public static RewriteRule Create<T1, TResult>(Expression<Func<T1, TResult>> find, Expression<Func<T1, TResult>> replace = null, Func<Match, Expression, bool> condition = null, TransformDelegate transform = null) => new RewriteRule(find, replace, condition, transform);
        public static RewriteRule Create<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> find, Expression<Func<T1, T2, TResult>> replace = null, Func<Match, Expression, bool> condition = null, TransformDelegate transform = null) => new RewriteRule(find, replace, condition, transform);
        public static RewriteRule Create<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> find, Expression<Func<T1, T2, T3, TResult>> replace = null, Func<Match, Expression, bool> condition = null, TransformDelegate transform = null) => new RewriteRule(find, replace, condition, transform);

        /// <summary>
        /// Expresión que se tiene que encontrar
        /// </summary>
        public LambdaExpression Find { get; }

        /// <summary>
        /// Expresión que se tiene que reemplazar, los argumentos del Replace encajan por orden con los de Find.
        /// Si es null no se realiza el reemplazo, pero aún así se puede realizar el transform
        /// </summary>
        public LambdaExpression Replace { get; }

        /// <summary>
        /// Condición que se debe de cumplir para que se aplique la regla. Si es null no se verifica la condición
        /// </summary>
        public Func<Match, Expression,  bool> Condition { get; }

        /// <summary>
        /// (match, expr, visit) => expr
        /// Transform que se aplica a la regla después de aplicar el reemplazo
        /// </summary>
        public TransformDelegate Transform { get; }
    }
}
