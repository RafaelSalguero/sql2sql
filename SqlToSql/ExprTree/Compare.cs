using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.ExprTree
{
    public static class CompareExpr
    {
        /// <summary>
        /// Sequence equal similar al de .NET pero acepta un lambda para realizar la comparación
        /// </summary>
        public static bool SequenceEqual<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second, Func<T1, T2, bool> comparer)
        {
            if (first == null)
                throw new NullReferenceException("first");

            if (second == null)
                throw new NullReferenceException("second");

            using (IEnumerator<T1> e1 = first.GetEnumerator())
            using (IEnumerator<T2> e2 = second.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    if (!(e2.MoveNext() && comparer(e1.Current, e2.Current)))
                        return false;
                }

                if (e2.MoveNext())
                    return false;
            }

            return true;
        }


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
