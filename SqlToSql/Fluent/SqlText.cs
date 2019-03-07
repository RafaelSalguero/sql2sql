using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SqlToSql.ExprTree;

namespace SqlToSql.Fluent
{

    public class SqlText
    {
        public static bool ExprEquals(Expression a, Expression b)
        {
            if (a is MemberExpression memA && b is MemberExpression memB)
            {
                return ExprEquals(memA.Expression, memB.Expression) && memA.Member == memB.Member;
            }
            return a == b;
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

        public static string SubqueryToString(IFromListItem fromListItem)
        {
            if (fromListItem is SqlTable table)
            {
                return $"\"{table.Name}\"";
            }
            throw new ArgumentException($"No se pudo convertir a cadena {fromListItem}");
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

        public static string JoinToStr<T1, T2, TRet>(SqlJoin<T1, T2, TRet> join)
        {
            return JoinToStrM(join, x => null, x => null);
        }

        static Func<Expression, Expression> combineRepFunc(Func<Expression, Expression> a, Func<Expression, Expression> b)
        {
            return expr =>
            {
                var replaceWith = a(expr);
                return b(replaceWith ?? expr) ?? replaceWith;
            };
        }


        public  static string JoinToStrM<T1, T2, TRet>(SqlJoin<T1, T2, TRet> join, Func<Expression, Expression> upperRepFunc, Func<Expression, string> upperMap)
        {
            //Convertir el lado derecho del JOIN:
            var currOnPar = join.On.Parameters[0];
            var mapPar = join.Map.Parameters[0];
            var textAliases = ExtractAliases(join.Map.Body).Select(x => Expression.Property(currOnPar, x.Alias));
            Func<Expression, string> repStr = expr =>
             {
                 var repWith = textAliases.Where(x => ExprEquals(x, expr)).Select(x => x.Member.Name.ToUpper()).FirstOrDefault();
                 return repWith ?? upperMap(expr);
             };

            var repOn = ReplaceVisitor.Replace(join.On.Body, upperRepFunc);
            var currentOnStr = ExprToSql(repOn, repStr);

            var mapAliases = ExtractAliases(join.Map.Body);
            Func<Expression, Expression> currentAliasRepFunc = combineRepFunc(expr =>
            {
                if (expr == join.Map.Parameters[1] || expr == join.Map.Parameters[0])
                {
                    var alias = mapAliases.Where(x => x.Expr ==expr).FirstOrDefault();
                    if (alias == null)
                    {
                        throw new ArgumentException("El lado derecho del join debe de aparecer en la función map del mismo");
                    }
                    return Expression.Property(currOnPar, alias.Alias);
                }

                return null;
            }, upperRepFunc);


            var currentAlias = ExprToSql(ReplaceVisitor.Replace(join.Map.Parameters[1], currentAliasRepFunc), repStr);
            var right = $"JOIN {SubqueryToString(join.Right)} {currentAlias} ON {currentOnStr}";

            if (join.Left is ISqlJoin<T1> leftJoin)
            {
                //Agregar al upperMap el mapeo de este JOIN al ON del siguiente JOIN

                //el ON del LEFT encaja con el T1 del Map de este join
                var leftOnPar = leftJoin.On.Parameters[0];


                var rep = ReplaceVisitor.Replace(join.Map.Body, mapPar, leftOnPar);
                var aliases = ExtractAliases(rep);

                var replace = aliases.Select(x => new
                {
                    find = x.Expr,
                    rep = Expression.Property(currOnPar, x.Alias)
                }).ToList();

                Func<Expression, Expression> repFunc = combineRepFunc(
                    expr =>
                    {
                        return replace.FirstOrDefault(x => ExprEquals(x.find, expr))?.rep;
                    },
                    upperRepFunc);

                var method = typeof(SqlText).GetMethod(nameof(JoinToStrM)).MakeGenericMethod(join.Left.GetType().GetGenericArguments());
                var leftStr = method.Invoke(null, new object[] { join.Left, repFunc, repStr });
                return leftStr + "\r\n" + right;
            }
            else if (join.Left is SqlTable table)
            {
                var fromAlias = ExprToSql(ReplaceVisitor.Replace(join.Map.Parameters[0], currentAliasRepFunc), repStr);

                return $"FROM {SubqueryToString(table)} {fromAlias}" + "\r\n" + right;
            }

            throw new ArgumentException("FROM-LIST invalido");
        }

        /// <summary>
        /// Convierte una expresión a SQL
        /// </summary>
        public static string ExprToSql(Expression expr, Func<Expression, string> subStr)
        {
            string ToStr(Expression ex) => ExprToSql(ex, subStr);
            if (subStr(expr) is string strRep && strRep != null)
            {
                return strRep;
            }

            if (expr is BinaryExpression bin)
            {
                var ops = new Dictionary<ExpressionType, string>
                {
                    { ExpressionType.Equal, "=" }
                };

                if (ops.TryGetValue(bin.NodeType, out string opStr))
                {
                    return $"({ToStr(bin.Left)} {opStr} {ToStr(bin.Right)})";
                }
            }
            else if (expr is MemberExpression mem)
            {
                return $"{ToStr(mem.Expression)}.{mem.Member.Name}";
            }
            return expr.ToString();
        }

        /// <summary>
        /// Convierte un from-list a SQL
        /// </summary>
        public static string FromList<T>(FromList<T> fromList)
        {
            return "";
        }
    }
}
