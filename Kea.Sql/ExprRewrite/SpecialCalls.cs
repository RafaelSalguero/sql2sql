using System;
using System.Linq.Expressions;

namespace KeaSql.ExprRewrite
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
        public static T Constant<T>(T x) => throw new RewriteSpecialCallException();

        /// <summary>
        /// Indica que la expresión no debe de ser una constante
        /// </summary>
        public static T NotConstant<T>(T x) => throw new RewriteSpecialCallException();

        /// <summary>
        /// Indica un operador binario
        /// </summary>
        /// <param name="op">Sólo encajar con este tipo de expresión o null para encajar con cualquiera</param>
        public static TRet Operator<TA, TB, TRet>(TA left, TB right, ExpressionType op) => throw new RewriteSpecialCallException();

        /// <summary>
        /// Indica un operador unario
        /// </summary>
        /// <param name="op">Sólo encajar con este tipo de expresión o null para encajar con cualquiera</param>
        public static TRet Operator<TA, TRet>(TA operand, ExpressionType? op) => throw new RewriteSpecialCallException();

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
