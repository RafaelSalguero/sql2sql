using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SqlToSql.ExprTree;
using SqlToSql.Fluent;

namespace SqlToSql.SqlText
{
    public static class SqlFromList
    {
        class ExpressionAlias
        {
            public ExpressionAlias(PropertyInfo alias, Expression expr)
            {
                Alias = alias;
                Expr = expr;
            }

            public PropertyInfo Alias { get; }
            public Expression Expr { get; }
        }

        static string SubqueryToString(IFromListItemTarget fromListItem)
        {
            if (fromListItem is SqlTable table)
            {
                return $"\"{table.Name}\"";
            }
            throw new ArgumentException($"No se pudo convertir a cadena {fromListItem}");
        }

        static IReadOnlyList<ExpressionAlias> ExtractAliases(Expression expr)
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


        class ExprRep
        {
            public ExprRep(Expression find, Expression rep)
            {
                Find = find;
                Rep = rep;
            }

            public Expression Find { get; }
            public Expression Rep { get; }
        }

        class ExprAliasList
        {
            public ExprAliasList(IReadOnlyList<ExprRep> items, Expression leftParam, Expression currParam, Expression leftOn)
            {
                Items = items;
                LeftParam = leftParam;
                CurrParam = currParam;
                LeftOn = leftOn;
            }

            public IReadOnlyList<ExprRep> Items { get; }
            public Expression LeftParam { get; }
            public Expression CurrParam { get; }
            public Expression LeftOn { get; }
        }

        class JoinAlias
        {
            public JoinAlias(Expression find, Expression replace, string alias)
            {
                Find = find;
                Replace = replace;
                Alias = alias;
            }

            public Expression Find { get; }
            public Expression Replace { get; }
            public string Alias { get; }
        }

        static string ExtractMemberStr(Expression ex)
        {
            if (ex is ParameterExpression param)
                return param.Name;
            else if (ex is MemberExpression member)
                return $"\"{member.Member.Name}\"";
            else
                throw new ArgumentException($"No se puede obtener el nombre de la expresión {ex}");
        }

        static Expression ReplaceExprList(Expression expr, IEnumerable<ExprRep> items)
        {
            Func<Expression, Expression> rep = ex => items.Where(x => CompareExpr.ExprEquals(x.Find, ex)).Select(x => x.Rep).FirstOrDefault();
            return ReplaceVisitor.Replace(expr, rep);
        }

        static IReadOnlyList<IReadOnlyList<JoinAlias>> ExtractJoinAliases(IFromListItem join)
        {
            var tree = ExtractJoinAliasTree(join);

            var ret = new List<List<JoinAlias>>();
            foreach (var level in tree)
            {
                ret.Add(new List<JoinAlias>());
                foreach (var rep in level.Items)
                {
                    if (!ret.SelectMany(x => x).Any(x => x.Find == rep.Find))
                    {
                        //Agregar el rep:
                        var exAlias = ret.SelectMany(x => x).Where(x => CompareExpr.ExprEquals(x.Replace, rep.Rep)).Select(x => x.Alias).FirstOrDefault();
                        string alias;
                        if (exAlias != null)
                        {
                            alias = exAlias;
                        }
                        else
                        {
                            var memberAlias = ExtractMemberStr(rep.Rep);
                            alias = memberAlias;
                            //Si el alias esta repetido, le ponemos un numero consecutivo
                            for (var i = 1; i < 1000 && ret.SelectMany(x => x).Where(x => x.Alias == alias).Any(); i++)
                            {
                                alias = $"\"{memberAlias.Trim('"') + i}\"";
                            }
                        }
                        ret[ret.Count - 1].Add(new JoinAlias(rep.Find, rep.Rep, alias));
                    }
                }
            }
            return ret;
        }

        static IReadOnlyList<ExprAliasList> ExtractJoinAliasT(IFromListItem left, Expression leftParam, Expression rightParam, Expression onParam, Expression mapExprBody)
        {
            var subRet = ExtractJoinAliasTree(left);
            var leftOnParam = subRet[0].CurrParam;

            var currAliases = mapExprBody == null ? new ExpressionAlias[0] : ExtractAliases(mapExprBody);
            var ret = new List<ExprAliasList>();

            var mapAliases = currAliases.Select(x => new ExprRep(

                         find: Expression.Property(onParam, x.Alias),
                         rep: Expression.Property(onParam, x.Alias)
                     ))
                     .ToList();

            //Encontrar el alias del left:
            var rightAlias = currAliases.Where(x => x.Expr == rightParam).Select(x => new ExprRep(x.Expr, Expression.Property(onParam, x.Alias))).FirstOrDefault();
            if (rightAlias != null)
            {
                mapAliases.Add(rightAlias);
            }

            var currentExprAlias = new ExprAliasList(mapAliases, leftParam, onParam, null);
            ret.Add(currentExprAlias);


            var repList = currAliases.Select(x => new ExprRep(
                    find: ReplaceVisitor.Replace(x.Expr, leftParam, leftOnParam),
                    rep: Expression.Property(onParam, x.Alias)
                    ))
                    .ToList();

            //Sustituir todos los subRet:
            var subRetSubs = subRet.Select(list =>
            new ExprAliasList(
                items: list.Items.Select(item => new ExprRep(
                    item.Find,
                    ReplaceExprList(item.Rep, repList)

                    )).ToList(),
                leftParam: list.LeftParam,
                currParam: list.CurrParam,
                leftOn: null
                ))
                .ToList()
                ;
            ret.AddRange(subRetSubs);
            //Agregar el alias del lado izquierdo:
            var leftAlias = currAliases.Where(x => x.Expr == leftParam).Select(x => new ExprRep(x.Expr, Expression.Property(onParam, x.Alias))).FirstOrDefault();
            if (leftAlias != null)
            {
                var fromAlias = new ExprAliasList(new[] { leftAlias }, leftParam, onParam, null);
                ret.Add(fromAlias);
            }

            return ret;
        }

        static IReadOnlyList<ExprAliasList> ExtractJoinAliasTree(IFromListItem fromItem)
        {
            if (fromItem is ISqlFromListAlias alias)
            {
                var mapParam = alias.Map.Parameters[0];
                var onParam = Expression.Parameter(alias.Map.Body.Type, mapParam.Name);
                return ExtractJoinAliasT(alias.From, mapParam, null, onParam, alias.Map.Body);
            }
            else if (fromItem is ISqlJoin join)
            {
                var leftParam = join.Map.Parameters[0];
                var rightParam = join.Map.Parameters[1];
                var onParam = join.On.Parameters[0];

                return ExtractJoinAliasT(join.Left, leftParam, rightParam, onParam, join.Map.Body);
            }
            else if (fromItem is ISqlFrom from)
            {
                return new[] { new ExprAliasList(new ExprRep[0], null, null, null) };
            }
            throw new ArgumentException();
        }

        public class FromListToStrResult
        {
            public FromListToStrResult(string sql, bool named)
            {
                Sql = sql;
                Named = named;
            }

            public string Sql { get; }
            /// <summary>
            /// Si el from list tiene aliases
            /// </summary>
            public bool Named { get; }
        }

        public static string FromListTargetToStr(IFromListItemTarget item)
        {
            if (item is SqlTable table)
            {
                return SubqueryToString(table);
            }
            else if (item is ISqlSelect select)
            {
                return $"(\r\n{SqlSelect.SelectToString(select.Clause)}\r\n)";
            }

            throw new ArgumentException("El from item target debe de ser una tabla o un select");
        }

        /// <summary>
        /// Convierte un from-list a SQL
        /// </summary>
        /// <param name="item"></param>
        /// <param name="upperAlias">El nombre que se le da a todo el from list, sólo aplica en caso de que el from list sea un from</param>
        /// <returns></returns>
        public static FromListToStrResult FromListToStr(IFromListItem item, string upperAlias)
        {
            var alias = ExtractJoinAliases(item).SelectMany(x => x).Select(x => new
            {
                expr = x.Find,
                alias = x.Alias
            });
            Func<Expression, string> replaceMembers = ex =>
            alias.Where(x => CompareExpr.ExprEquals(x.expr, ex)).Select(x => x.alias).FirstOrDefault();
            Func<Expression, string> toSql = ex => SqlExpression.ExprToSql(ex, replaceMembers);

            return JoinToStr(item, toSql, upperAlias);
        }


        static FromListToStrResult JoinToStr(IFromListItem item, Func<Expression, string> toSql, string upperAlias)
        {
            if (item is ISqlJoin join)
            {
                var currentAlias = toSql(join.Map.Parameters[1]);
                var currentOnStr = toSql(join.On.Body);
                var right = $"JOIN {FromListTargetToStr(join.Right)} {currentAlias} ON {currentOnStr}";

                var leftAlias = toSql(join.Map.Parameters[0]);
                var leftStr = JoinToStr(join.Left, toSql, leftAlias).Sql;
                return new FromListToStrResult(leftStr + "\r\n" + right, true);
            }
            else if (item is ISqlFrom from)
            {
                return new FromListToStrResult($"FROM {FromListTargetToStr(from.Target)} {upperAlias}", false);
            }
            else if (item is ISqlFromListAlias alias)
            {
                return JoinToStr(alias.From, toSql, upperAlias);
            }

            throw new ArgumentException("El from-item debe de ser un JOIN, FROM o Alias()");
        }



        /// <summary>
        /// Convierte un from-list a SQL
        /// </summary>
        public static string FromList<T>(FromListFrom<T> fromList)
        {
            return "";
        }
    }
}
