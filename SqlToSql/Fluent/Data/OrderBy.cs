using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Fluent.Data
{
 
    public interface IOrderByExpr
    {
        LambdaExpression Expr { get; }
        OrderByOrder Order { get; }
        OrderByNulls? Nulls { get; }
    }

    public class OrderByExpr<TIn> : IOrderByExpr
    {
        public OrderByExpr(Expression<Func<TIn, object>> expr, OrderByOrder order, OrderByNulls? nulls)
        {
            Expr = expr;
            Order = order;
            Nulls = nulls;
        }

        public Expression<Func<TIn, object>> Expr { get; }
        public OrderByOrder Order { get; }
        public OrderByNulls? Nulls { get; }

        LambdaExpression IOrderByExpr.Expr => Expr;
    }
}
