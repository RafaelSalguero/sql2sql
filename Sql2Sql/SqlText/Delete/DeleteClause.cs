using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Fluent.Data
{
    /// <summary>
    /// Data needed to generate a DELETE statement
    /// </summary>
    public class DeleteClause
    {
        public DeleteClause(string table, bool only, IReadOnlyList<string> @using, bool usingNamed, LambdaExpression where, LambdaExpression returning)
        {
            Table = table;
            Only = only;
            Using = @using;
            UsingNamed = usingNamed;
            Where = where;
            Returning = returning;
        }

        /// <summary>
        /// Table name
        /// </summary>
        public string Table { get; }

        /// <summary>
        /// ONLY clause, if true, rows from inheriting tables are not deleted
        /// </summary>
        public bool Only { get; }

        /// <summary>
        /// Extra table expressions allowing columns. Can't be null
        /// </summary>
        public IReadOnlyList<string> Using { get; }

        /// <summary>
        /// If true the using argument of the where condition would be an object where every property references
        /// a using table, if false the argument itself would the the table.
        /// 
        /// Similar to <see cref="SqlText.SqlExprParams.FromListNamed"/>
        /// </summary>
        public bool UsingNamed { get; }

        /// <summary>
        /// Two argument lambda expression of the delete condition.
        /// The first argument references a row of the table
        /// The second argument references a row or the object of references of the using type, depending on the <see cref="UsingNamed"/> property.
        /// 
        /// The order of the properties of the second argument type matches <see cref="Using"/> items
        /// 
        /// Can be null
        /// </summary>
        public LambdaExpression Where { get; }

        /// <summary>
        /// Expression of the RETUNRING clause, have the same arguments as <see cref="Where"/>
        /// 
        /// Can be null
        /// </summary>
        public LambdaExpression Returning { get; }
    }
}
