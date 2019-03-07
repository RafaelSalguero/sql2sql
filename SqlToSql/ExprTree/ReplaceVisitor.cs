using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SqlToSql.ExprTree
{
    /// <summary>
    /// Remplaza una expresión con otra
    /// </summary>
    public class ReplaceVisitor : ExpressionVisitor
    {
        public ReplaceVisitor(Func<Expression, Expression> replace)
        {
            this.replace = replace;
        }

        Func<Expression, Expression> replace;

        public static Expression Replace(Expression Expression, Expression Find, Expression ReplaceWith)
        {
            var V = new ReplaceVisitor(x => x == Find ? ReplaceWith : null);
            return V.Visit(Expression);
        }

        public static Expression Replace(Expression Expression, Dictionary<Expression, Expression> dic)
        {
            var V = new ReplaceVisitor(x => dic.TryGetValue(x, out Expression ret) ? ret : null);
            return V.Visit(Expression);
        }

        public static Expression Replace(Expression Expression, Func<Expression, Expression> replace)
        {
            var V = new ReplaceVisitor(replace);
            return V.Visit(Expression);
        }

        /// <summary>
        /// True if any match has been found
        /// </summary>
        public bool Any = false;
        public override Expression Visit(Expression node)
        {
            if (node == null) return null;
            var ret = replace(node);
            if (ret != null)
            {
                Any = true;
                return ret;
            }
            else
                return base.Visit(node);
        }
    }

}
