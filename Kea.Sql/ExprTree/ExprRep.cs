using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.ExprTree
{
    public static class ExprReplace
    {
        public class ExprRep
        {
            public ExprRep(Expression find, Expression rep)
            {
                Find = find;
                Rep = rep;
            }

            public Expression Find { get; }
            public Expression Rep { get; }

            public override string ToString()
            {
                return Find.ToString() + " ---> " + Rep.ToString();
            }
        }

        public static Expression ReplaceExprList(Expression expr, IEnumerable<ExprRep> items)
        {
            Func<Expression, Expression> rep = ex =>
            {
                var toRep = ex;
                Expression result = null;
                while (true)
                {
                    var find = items.Where(x => CompareExpr.ExprEquals(x.Find, toRep)).Select(x => x.Rep).FirstOrDefault();
                    if (find != null)
                    {
                        toRep = find;
                        result = find;
                    }
                    else
                    {
                        break;
                    }
                }
                return result;
            };
            return ReplaceVisitor.Replace(expr, rep);
        }

        public class ExpressionAlias
        {
            public ExpressionAlias(PropertyInfo alias, Expression expr)
            {
                Alias = alias;
                Expr = expr;
            }

            public PropertyInfo Alias { get; }
            public Expression Expr { get; }
        }


        public static IReadOnlyList<ExpressionAlias> ExtractAliases(Expression expr)
        {
            if (expr is MemberInitExpression mem)
            {
                return mem.Bindings.Select(bind =>
                {
                    if (bind is MemberAssignment assig && assig.Member is PropertyInfo prop)
                    {
                        return new ExpressionAlias(prop, assig.Expression);
                    }
                    throw new ArgumentException("El binding debe de ser de assignment");
                }).ToList();
            }
            else if (expr is NewExpression cons)
            {
                var consPars = cons.Constructor.GetParameters();
                var typeProps = cons.Type.GetProperties();
                return cons.Arguments.Select((arg, i) =>
                {
                    var param = consPars[i].Name;
                    var prop = typeProps.Where(x => x.Name.ToLower() == param.ToLower()).FirstOrDefault();
                    if (prop == null)
                        throw new ArgumentException($"No se encontró ninguna propiedad en el tipo {cons.Type.Name} que en caje con el parametro {param}");

                    return new ExpressionAlias(prop, arg);
                }).ToList();
            }
            throw new ArgumentException("La expresión debe de ser de inicialización");
        }
    }
}
