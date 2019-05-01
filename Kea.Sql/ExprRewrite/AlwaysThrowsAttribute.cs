using System;
using System.Collections.Generic;
using System.Text;

namespace KeaSql.ExprRewrite
{
    /// <summary>
    /// Indica que este método siempre lanza una excepción al ser llamado, marque estos métodos para que el evalue más rápido las llamadas de los mismos
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class AlwaysThrowsAttribute : Attribute
    {
        public AlwaysThrowsAttribute()
        {
        }
    }

    /// <summary>
    /// Indica que este método toma un argumento y devuelve como resultado ese mismo argumento
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class IdempotentAttribute : Attribute
    {
        public IdempotentAttribute()
        {
        }
    }

    /// <summary>
    /// Indica que este método siempre devuelve null
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class AlwaysNullAttribute : Attribute
    {
        public AlwaysNullAttribute()
        {
        }
    }
}
