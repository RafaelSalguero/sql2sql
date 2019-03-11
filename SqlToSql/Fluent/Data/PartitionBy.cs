using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SqlToSql.Fluent.Data
{
    public interface IPartitionBy
    {
        LambdaExpression Expr { get; }
    }
    public class PartitionByExpr<TIn> : IPartitionBy
    {
        public PartitionByExpr(Expression<Func<TIn, object>> expr)
        {
            Expr = expr;
        }

        public Expression<Func<TIn, object>> Expr { get; }
        LambdaExpression IPartitionBy.Expr => Expr;
    }
}
