using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Fluent
{
    /// <summary>
    /// Indica que esta función no se puede llamar ya que su función es ser traducida a SQL
    /// </summary>
    public class SqlFunctionException : ArgumentException  
    {
        public SqlFunctionException() : base("Esta función sólo puede ser utilizada dentro de una expresión SQL, no se puede ejecutar directamente")
        {

        }
    }
}
