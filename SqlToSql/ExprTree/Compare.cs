﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SqlToSql.ExprTree
{
    public static class CompareExpr
    {
        public static bool ExprEquals(Expression a, Expression b)
        {
            if (a is MemberExpression memA && b is MemberExpression memB)
            {
                return ExprEquals(memA.Expression, memB.Expression) && memA.Member == memB.Member;
            }
            return a == b;
        }
    }
}