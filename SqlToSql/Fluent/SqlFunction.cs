using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlToSql.Fluent
{
    /// <summary>
    /// Indica que esta es una función de SQL
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class SqlFunctionAttribute : Attribute
    {
        // This is a positional argument
        public SqlFunctionAttribute(string sqlName)
        {
            this.SqlName = sqlName;

        }
        public string SqlName { get; }

    }
}
