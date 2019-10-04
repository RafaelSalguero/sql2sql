using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Fluent.Data
{
    /// <summary>
    /// Indicates the type of an UNION clause
    /// </summary>
    public enum UnionType
    {
        Union,
        Intersect,
        Except
    }

    /// <summary>
    /// If an UNION have the ALL or DISTINCT clause
    /// </summary>
    public enum UnionUniqueness
    {
        All,
        Distinct,
    }

    /// <summary>
    /// A UNION clause
    /// </summary>
    public class UnionClause
    {
        public UnionClause(UnionType type, UnionUniqueness uniqueness, ISqlSelect select)
        {
            Type = type;
            Uniqueness = uniqueness;
            Select = select;
        }

        /// <summary>
        /// The type of the UNION
        /// </summary>
        public UnionType Type { get; }

        /// <summary>
        /// ALL or DISTINCT clause
        /// </summary>
        public UnionUniqueness Uniqueness { get; }

        /// <summary>
        /// The SELECT clasue of the union
        /// </summary>
        public ISqlSelect Select { get; }
    }
}
