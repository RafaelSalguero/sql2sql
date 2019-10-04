using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Fluent.Data
{
    public class OrderByExpr  
    {
        public OrderByExpr(LambdaExpression expr, OrderByOrder order, OrderByNulls? nulls)
        {
            Expr = expr;
            Order = order;
            Nulls = nulls;
        }

        public LambdaExpression Expr { get; }
        public OrderByOrder Order { get; }
        public OrderByNulls? Nulls { get; }
    }
}
