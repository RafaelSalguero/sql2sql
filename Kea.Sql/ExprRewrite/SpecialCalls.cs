using System;

namespace KeaSql.ExprRewrite
{
    /// <summary>
    /// Llamadas que representan expresiones especiales para las reglas de rewrite
    /// </summary>
    public static class RewriteSpecial
    {
        public class AnyType { }

        /// <summary>
        /// Representa un tipo que encaja con cualquiera
        /// </summary>
        public class Type1 : AnyType { }
        public class Type2 : AnyType { }
        public class Type3 : AnyType { }

        class RewriteSpecialCallException : System.ArgumentException
        {
            public RewriteSpecialCallException() : base("Los metodos de RewriteSpecialCalls sólo se deben de usar en el Find de los RewriteRules") { }
        }

        /// <summary>
        /// Indica que la expresión debe de ser una constante
        /// </summary>
        public static T Constant<T>(T x) => throw new RewriteSpecialCallException();

        /// <summary>
        /// Indica que la expresión no debe de ser una constante
        /// </summary>
        public static T NotConstant<T>(T x) => throw new RewriteSpecialCallException();

        /// <summary>
        /// Indica una llamada a un metodo con cierto nombre, cualquier método que encaje con ese nombre pasará el patron
        /// </summary>
        /// <param name="type">Si es null sólo filtra por el nombre del método</param>
        /// <param name="methodName">Nombre del método</param>
        /// <param name="instance">Objeto al que se le realiza la llamada o null para indicar un método estático</param>
        public static TResult Call<TResult, TInstance>(Type type, string methodName, TInstance instance) => throw new RewriteSpecialCallException();


        /// <summary>
        /// Indica una llamada a un metodo con cierto nombre, cualquier método que encaje con ese nombre pasará el patron
        /// </summary>
        /// <param name="type">Si es null sólo filtra por el nombre del método</param>
        /// <param name="methodName">Nombre del método</param>
        public static TResult Call<TResult>(Type type, string methodName) => throw new RewriteSpecialCallException();
    }
}
