using System.Linq.Expressions;

namespace SqlToSql.ExprTree
{
    internal class ExpressionEnumeration
    {
        private Expression b;

        public ExpressionEnumeration(Expression b)
        {
            this.b = b;
        }
    }
}