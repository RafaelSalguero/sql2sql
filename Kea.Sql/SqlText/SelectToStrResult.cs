using KeaSql.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.SqlText
{
    /// <summary>
    /// Resultado de converir un <see cref="ISqlStatement"/> a string
    /// </summary>
    public abstract class StatementToStrResult
    {
        public StatementToStrResult(string sql)
        {
            Sql = sql;
        }

        public string Sql { get; }
    }

    /// <summary>
    /// Resultado de convertir un <see cref="ISqlInsertHasClause"/> sin returning a string
    /// </summary>
    public class InsertNoReturningStrResult : StatementToStrResult
    {
        public InsertNoReturningStrResult(string sql) : base(sql)
        {
        }
    }

    /// <summary>
    /// Resultado de convertir un <see cref="SqlTable"/> o un <see cref="ISqlTableRefRaw"/> a string
    /// </summary>
    public class TableToStrResult : StatementToStrResult
    {
        public TableToStrResult(string sql) : base(sql)
        {
        }
    }

    /// <summary>
    /// Resultado de convertir un <see cref="ISqlQuery"/> a string
    /// </summary>
    public class QueryToStrResult : StatementToStrResult
    {
        public QueryToStrResult(string sql) : base(sql)
        {
        }
    }

    /// <summary>
    /// Resultado de convetir un <see cref="ISqlQuery{TOut}"/> a string, este incluye las columnas
    /// </summary>
    public class QueryColsToStrResult : QueryToStrResult
    {
        public QueryColsToStrResult(string sql, IReadOnlyList<string> columns) : base(sql)
        {
            Columns = columns;
        }

        /// <summary>
        /// En caso de que el FromListTarget fuera un query que no es RAW (ya sea un SELECT o un statement con RETURNING)
        /// son los nombres de las columnas, en otro caso es null
        /// </summary>
        public IReadOnlyList<string> Columns { get; }
    }

    /// <summary>
    /// Resultado de convertir un <see cref="ISqlInsertReturning{T}"/> a string 
    /// </summary>
    public class InsertReturningToStr : QueryColsToStrResult
    {
        public InsertReturningToStr(string sql, IReadOnlyList<string> columns) : base(sql, columns)
        {
        }
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
