using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Fluent.Data
{
    public interface IGroupByExpr
    {
        LambdaExpression Expr { get; }
    }

    public class GroupByExpr<TIn> : IGroupByExpr
    {
        public GroupByExpr(Expression<Func<TIn, object>> expr)
        {
            Expr = expr;
        }

        public Expression<Func<TIn, object>> Expr { get; }
        LambdaExpression IGroupByExpr.Expr => Expr;
    }
}
