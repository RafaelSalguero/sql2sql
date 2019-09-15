using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql
{
    /// <summary>
    /// Un parametro de SQL nombrado
    /// </summary>
    public class SqlParam
    {
        public SqlParam(string name, object value, Type type)
        {
            Name = name;
            Value = value;
            Type = type;
        }

        /// <summary>
        /// Nombre del parámetro
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Valor del parámetro
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Tipo del parámetro
        /// </summary>
        public Type Type { get; }
    }

    /// <summary>
    /// El SQL de un query con los valores de los parametros 
    /// </summary>
    public class SqlResult
    {
        public SqlResult(string sql, IReadOnlyList<SqlParam> @params)
        {
            Sql = sql;
            Params = @params;
        }

        /// <summary>
        /// El SQL del query
        /// </summary>
        public string Sql { get; }

        /// <summary>
        /// Los parametros del query
        /// </summary>
        public IReadOnlyList<SqlParam> Params { get; }
    }
}
