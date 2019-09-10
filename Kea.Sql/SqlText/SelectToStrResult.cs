using KeaSql.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.SqlText
{
    public enum FromListTargetType
    {
        /// <summary>
        /// El elemento es un query
        /// </summary>
        Select,
        /// <summary>
        /// El elemento es una referencia a una tabla
        /// </summary>
        Table
    }

    /// <summary>
    /// Resultado de convertir un <see cref="IFromListItemTarget"/> a string
    /// </summary>
    public class FromListTargetToStrResult
    {
        public FromListTargetToStrResult(string sql, IReadOnlyList<string> columns, FromListTargetType type)
        {
            Sql = sql;
            Columns = columns;
            Type = type;
        }

        /// <summary>
        /// SQL convertido
        /// </summary>
        public string Sql { get; }

        /// <summary>
        /// En caso de que el FromListTarget fuera un SELECT que no es RAW, son los nombres de las columnas, en otro caso es null
        /// </summary>
        public IReadOnlyList<string> Columns { get; }

        /// <summary>
        /// Si el elemento convertido es un SELECT, si no, entonces es una referencia a una tabla
        /// </summary>
        public FromListTargetType Type { get; }
    }

    /// <summary>
    /// Resultado de convertir una clausula de SELECT a string
    /// </summary>
    public class SelectToStrResult
    {
        public SelectToStrResult(string sql, IReadOnlyList<string> columns, bool scalar)
        {
            Sql = sql;
            Columns = columns;
            Scalar = scalar;
        }

        /// <summary>
        /// El SQL de todo el SELECT
        /// </summary>
        public string Sql { get; }

        /// <summary>
        /// Las columnas devueltas por el SELECT
        /// </summary>
        public IReadOnlyList<string> Columns { get; }

        /// <summary>
        /// Si el SELECT devuelve una sola columna escalar
        /// </summary>
        public bool Scalar { get; }
    }

    /// <summary>
    /// Un valor SQL y el nombre de la columna que corresponde
    /// </summary>
    public class ValueCol
    {
        public ValueCol(string value, string column)
        {
            Value = value;
            Column = column;
        }

        /// <summary>
        /// Valor en forma de SQL
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Nombre de la columna. Para un SELECT escalar esto será null
        /// </summary>
        public string Column { get; }
    }

    /// <summary>
    /// Resultado de convertir la expresión de proyección de un SELECT a string
    /// </summary>
    public class SelectExprToStrResult
    {
        public SelectExprToStrResult(IReadOnlyList<ValueCol> values, bool scalar)
        {
            Values = values;
            Scalar = scalar;
        }

        /// <summary>
        /// Valores y columnas del SELECT
        /// </summary>
        public IReadOnlyList<ValueCol> Values { get; }

        /// <summary>
        /// Si el select devuelve un solo valor escalar
        /// </summary>
        public bool Scalar { get; }
    }
}
