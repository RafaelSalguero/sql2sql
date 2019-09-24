using System;
using System.Linq.Expressions;

namespace Sql2Sql.ExprRewrite
{
    /// <summary>
    /// Tipos comodín
    /// </summary>
    public static class RewriteTypes
    {
        public interface WildType { }
        public interface MatchType { }

        /// <summary>
        /// Any struct
        /// </summary>
        public struct S : WildType { }

        /// <summary>
        /// Any class
        /// </summary>
        public class C : WildType { }

        public class C1 : MatchType { }
        public class C2 : MatchType { }
        public class C3 : MatchType { }
        public class C4 : MatchType { }
        public class C5 : MatchType { }

        public struct S1 : MatchType { }
        public struct S2 : MatchType { }
        public struct S3 : MatchType { }
        public struct S4 : MatchType { }
        public struct S5 : MatchType { }
    }

    /// <summary>
    /// Llamadas que representan expresiones especiales para las reglas de rewrite
    /// </summary>
    public static class RewriteSpecial
    {



        class RewriteSpecialCallException : System.ArgumentException
        {
            public RewriteSpecialCallException() : base("Los metodos de RewriteSpecialCalls sólo se deben de usar en el Find de los RewriteRules") { }
        }

        /// <summary>
        /// Indica que la expresión debe de ser una constante
        /// </summary>
        [AlwaysThrows]
        public static T Constant<T>(T x) => throw new RewriteSpecialCallException();

        /// <summary>
        /// Indica que la expresión debe de ser un parametro
        /// </summary>
        [AlwaysThrows]
        public static T Parameter<T>(T x) => throw new RewriteSpecialCallException();

        /// <summary>
        /// Indica que la expresión no debe de ser una constante
        /// </summary>
        [AlwaysThrows]
        public static T NotConstant<T>(T x) => throw new RewriteSpecialCallException();

        /// <summary>
        /// Indica un operador binario
        /// </summary>
        /// <param name="op">Sólo encajar con este tipo de expresión o null para encajar con cualquiera</param>
        [AlwaysThrows]
        public static TRet Operator<TA, TB, TRet>(TA left, TB right, ExpressionType op) => throw new RewriteSpecialCallException();

        /// <summary>
        /// Indica un operador unario
        /// </summary>
        /// <param name="op">Sólo encajar con este tipo de expresión o null para encajar con cualquiera</param>
        [AlwaysThrows]
        public static TRet Operator<TA, TRet>(TA operand, ExpressionType op) => throw new RewriteSpecialCallException();

        /// <summary>
        /// Indica una llamada a un metodo con cierto nombre, cualquier método que encaje con ese nombre pasará el patron
        /// </summary>
        /// <param name="type">Si es null sólo filtra por el nombre del método, si no, es el tipo padre del método</param>
        /// <param name="methodName">Nombre del método</param>
        /// <param name="instance">Objeto al que se le realiza la llamada o null para indicar un método estático</param>
        [AlwaysThrows]
        public static TResult Call<TResult, TInstance>(Type type, string methodName, TInstance instance) => throw new RewriteSpecialCallException();


        /// <summary>
        /// Indica una llamada a un metodo con cierto nombre, cualquier método que encaje con ese nombre pasará el patron
        /// </summary>
        /// <param name="type">Si es null sólo filtra por el nombre del método</param>
        /// <param name="methodName">Nombre del método</param>
        [AlwaysThrows]
        public static TResult Call<TResult>(Type type, string methodName) => throw new RewriteSpecialCallException();

        /// <summary>
        /// Indica que la expresión dentro del Atom() sólo se podrá sustituir en el primer nivel, no se realizarán 
        /// sustituciones en las subexpresiones de la misma
        /// </summary>
        [Idempotent]
        public static T Atom<T>(T x) => x;

        [Idempotent]
        public static T Visit<T>(T x) => x;

        /// <summary>
        /// Aplica para la sustitución, indica que hay que aplicar la función transform a esta parte de la expresión
        /// </summary>
        [AlwaysThrows]
        public static T Transform<T>(T x, Func<Expression, Expression> transform) => throw new RewriteSpecialCallException();
    }
}
