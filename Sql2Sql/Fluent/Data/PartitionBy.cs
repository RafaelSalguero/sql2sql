using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Fluent.Data
{
    public class PartitionByExpr 
    {
        public PartitionByExpr(LambdaExpression expr)
        {
            Expr = expr;
        }

        public LambdaExpression Expr { get; }
    }
   
}
