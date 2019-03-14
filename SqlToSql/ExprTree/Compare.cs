using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SqlToSql.ExprTree
{
    public static class CompareExpr
    {
        public static bool CompareMemberInfo(MemberInfo a, MemberInfo b)
        {
            return a.Module == b.Module && a.MetadataToken == b.MetadataToken;
        }

        public static bool ExprEquals(Expression a, Expression b)
        {
            if (a is MemberExpression memA && b is MemberExpression memB)
            {
                return ExprEquals(memA.Expression, memB.Expression) && CompareMemberInfo(memA.Member, memB.Member);
            }
            if(a is ConstantExpression consA && b is ConstantExpression consB)
            {
                return consA.Value == consB.Value;
            }
            return a == b;
        }
    }
}
