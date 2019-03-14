using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql
{
    /// <summary>
    /// Un parametro de SQL nombrado
    /// </summary>
    public class SqlParam
    {
        public SqlParam(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public object Value { get; }
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

        public string Sql { get; }
        public IReadOnlyList<SqlParam> Params { get; }
    }
}
