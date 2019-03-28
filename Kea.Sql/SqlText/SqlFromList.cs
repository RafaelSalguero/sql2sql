using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using KeaSql.ExprTree;
using KeaSql.Fluent;
using KeaSql.Fluent.Data;
using static KeaSql.ExprTree.ExprReplace;

namespace KeaSql.SqlText
{
    /// <summary>
    /// Convierte un FROM-LIST a SQL
    /// </summary>
    public static class SqlFromList
    {

        static string SubqueryToString(IFromListItemTarget fromListItem)
        {
            if (fromListItem is SqlTable table)
            {
                return $"\"{table.Name}\"";
            }
            throw new ArgumentException($"No se pudo convertir a cadena {fromListItem}");
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

        public class JoinAlias
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


        public static IReadOnlyList<IReadOnlyList<JoinAlias>> ExtractJoinAliases(IFromListItem join)
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
                        var exAlias = ret.SelectMany(x => x).Where(x =>
                            CompareExpr.ExprEquals(x.Replace, rep.Rep) || CompareExpr.ExprEquals(x.Find, rep.Rep)
                        ).Select(x => x.Alias).FirstOrDefault();
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
            if (left == null)
            {
                return new[] { new ExprAliasList(new ExprRep[0], leftParam, onParam, null) };
            }

            var subRet = ExtractJoinAliasTree(left);
            var leftOnParam = subRet[0].CurrParam;

            var currAliases = mapExprBody == null ? new ExpressionAlias[0] : ExtractAliases(mapExprBody);
            var ret = new List<ExprAliasList>();

            //Si el left no tiene OnParam, debe de existir el lado izquierdo en el mapeo
            var existirLeft = leftOnParam == null;
            if (existirLeft && !currAliases.Any(x => x.Expr == leftParam))
            {
                throw new ArgumentException($"El argumento '{leftParam}' debe de existir en el mapeo del ON del JOIN '{mapExprBody}' ya que el lado izquierdo es un from list que no esta nombrado");
            }

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
            else
            {
                //Reemplazar el lado izquierdo de este JOIN con el ON del JOIN izquierdo
                leftAlias = new ExprRep(leftParam, leftOnParam);
                var mapAlRep = currAliases.Select(x => new ExprRep(x.Expr, ReplaceVisitor.Replace(x.Expr, leftParam, leftOnParam))).ToList();
                var fromAlias = new ExprAliasList(mapAlRep, leftParam, onParam, null);
                ret.Add(fromAlias);
            }

            return ret;
        }

        static IReadOnlyList<ExprAliasList> ExtractJoinAliasTree(IFromListItem fromItem)
        {
            if (fromItem == null)
            {
                return new[] { new ExprAliasList(new ExprRep[0], null, null, null) };

            }

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
            else if (fromItem is ISqlFrom from || fromItem is ISqlTableRefRaw)
            {
                return new[] { new ExprAliasList(new ExprRep[0], null, null, null) };
            }
            throw new ArgumentException();
        }

        public class FromListToStrResult
        {
            public FromListToStrResult(string sql, bool named, IReadOnlyList<ExprStrAlias> aliases)
            {
                Sql = sql;
                Named = named;
                Aliases = aliases;
            }

            public string Sql { get; }

            /// <summary>
            /// Si el from list tiene aliases
            /// </summary>
            public bool Named { get; }

            /// <summary>
            /// Aliases de las expresiones del from
            /// </summary>
            public IReadOnlyList<ExprStrAlias> Aliases { get; }
        }

        /// <summary>
        /// Agrega los parentesis si subQ es true
        /// </summary>
        static string SubqueryParenthesis((string sql, bool subQ) fromList )
        {
            if (fromList.subQ)
                return $"(\r\n{SqlSelect.TabStr(fromList.sql)}\r\n)";
            else
                return fromList.sql;
        }


        /// <summary>
        /// Convierte un <see cref="IFromListItemTarget"/> a string, devuelve true si el elemento requiered de un alias
        /// </summary>
        /// <param name="item"></param>
        /// <param name="paramMode"></param>
        /// <param name="paramDic"></param>
        /// <returns></returns>
        public static (string sql, bool subQ) FromListTargetToStr(IFromListItemTarget item, ParamMode paramMode, SqlParamDic paramDic)
        {
            if (item is SqlTable table)
            {
                return (SubqueryToString(table), false);
            }
            else if (item is ISqlSelectExpr select)
            {
                return (SqlSelect.SelectToString(select.Clause, paramMode, paramDic), true);
            }
            else if (item is ISqlWithSelect withSelect)
            {
                var withSql = SqlWith.WithToSql(withSelect.With.With, withSelect.With.Param, paramMode, paramDic);
                var subquerySql = FromListTargetToStr(withSelect.Query, paramMode, paramDic).sql;
                var ret = $"{withSql}\r\n{subquerySql}";
                return (ret, true);
            }
            else if (item is ISqlTableRefRaw raw)
            {
                return (raw.Raw, false);
            }
            else if (item is ISqlSelectRaw subq)
            {
                return (SqlSelect.DetabStr(subq.Raw), true);
            }
            throw new ArgumentException("El from item target debe de ser una tabla o un select");
        }

        /// <summary>
        /// Indica un alias para una expresión
        /// </summary>
        public class ExprStrAlias
        {
            public ExprStrAlias(Expression expr, string alias)
            {
                Expr = expr;
                Alias = alias;
            }

            public Expression Expr { get; }
            public string Alias { get; }
        }

        /// <summary>
        /// Devuelve la cadena a la que corresponde la expresión o null en caso de que esta expresión no tenga ningún alias
        /// </summary>
        public static string ReplaceStringAliasMembers(Expression ex, IEnumerable<ExprStrAlias> alias)
        {
            return alias.Where(x => CompareExpr.ExprEquals(x.Expr, ex)).Select(x => x.Alias).FirstOrDefault();
        }

        /// <summary>
        /// Convierte un from-list a SQL, sin parámetros
        /// </summary>
        public static FromListToStrResult FromListToStrSP(IFromListItem item, string upperAlias, bool forceUpperAlias)
        {
            return FromListToStr(item, upperAlias, forceUpperAlias, ParamMode.None, new SqlParamDic());
        }

        /// <summary>
        /// Convierte un from-list a SQL
        /// </summary>
        /// <param name="item"></param>
        /// <param name="upperAlias">El nombre que se le da a todo el from list, sólo aplica en caso de que el from list sea un from</param>
        /// <returns></returns>
        public static FromListToStrResult FromListToStr(IFromListItem item, string upperAlias, bool forceUpperAlias, ParamMode paramMode, SqlParamDic paramDic)
        {
            var alias = ExtractJoinAliases(item).SelectMany(x => x).Select(x => new ExprStrAlias(x.Find, x.Alias)).ToList();

            var pars = new SqlExprParams(null, null, false, null, alias, paramMode, paramDic);
            Func<Expression, string> toSql = ex => SqlExpression.ExprToSql(ex, pars);

            var join = JoinToStr(item, toSql, alias, upperAlias, forceUpperAlias, paramMode, paramDic);
            return new FromListToStrResult(join.sql, join.named, alias);
        }

        static Expression RawSqlTableExpr(Type rawType, string sql)
        {
            var method = typeof(Sql).GetMethods().Where(x => x.Name == nameof(Sql.RawRowRef) && x.IsGenericMethod).Single();
            var mgen = method.MakeGenericMethod(rawType);

            var ret = Expression.Call(mgen, Expression.Constant(sql));
            return ret;
        }

        /// <summary>
        /// Reemplaza las ocurrencias de replace members en el cuerpo de un subquery por llamadas al Sql.Raw
        /// </summary>
        /// <param name="body"></param>
        /// <param name="replaceMembers"></param>
        /// <returns></returns>
        public static Expression ReplaceSubqueryBody(Expression body, IEnumerable<ExprStrAlias> replaceMembers)
        {
            //Sustituir con el replace members, con un RawSql
            Func<Expression, Expression> replaceRaw = (ex) =>
            {
                var sql = ReplaceStringAliasMembers(ex, replaceMembers);
                if (sql == null) return null;
                var ret = RawSqlTableExpr(ex.Type, sql);
                return ret;
            };
            var bodyRaw = ReplaceVisitor.Replace(body, replaceRaw);
            return bodyRaw;
        }

        /// <summary>
        /// Reemplaza las referencias a las tablas de los join anteriores en un LATERAL subquery con RawSQL, devuelve el body reemplazado
        /// </summary>
        public static Expression ReplaceSubqueryLambda(LambdaExpression subquery, Expression leftParam, IEnumerable<ExprStrAlias> replaceMembers)
        {
            var body = subquery.Body;
            var lateralParam = subquery.Parameters[0];

            //Reemplazar el parametro del lateral con el leftParam:
            var bodyLeft = ReplaceVisitor.Replace(body, lateralParam, leftParam);

            return ReplaceSubqueryBody(bodyLeft, replaceMembers);
        }

        static (string sql, bool named) JoinToStr(IFromListItem item, Func<Expression, string> toSql, IReadOnlyList<ExprStrAlias> replaceMembers, string upperAlias, bool forceUpperAlias, ParamMode paramMode, SqlParamDic paramDic)
        {
            if (item is ISqlJoin join)
            {
                var currentAlias = toSql(join.Map.Parameters[1]);
                var currentOnStr = toSql(join.On.Body);
                var leftParam = join.Map.Parameters[0];
                var leftAlias = ReplaceStringAliasMembers(leftParam, replaceMembers);
                var rightSubs = ReplaceSubqueryLambda(join.Right, leftParam, replaceMembers);
                var rightFunc = Expression.Lambda(rightSubs).Compile();
                var rightExec = (IFromListItemTarget)rightFunc.DynamicInvoke(new object[0]);

                var latStr = join.Lateral ? "LATERAL " : "";
                var typeStr =
                    join.Type == JoinType.Inner ? "" :
                    join.Type == JoinType.Left ? "LEFT " :
                    join.Type == JoinType.Right ? "RIGHT " :
                    join.Type == JoinType.Outter ? "OUTTER " :
                    join.Type == JoinType.Cross ? "CROSS " :
                    throw new ArgumentException("Join type " + join.Type + " invalido");

                var right = $"{typeStr}JOIN {latStr}{SubqueryParenthesis(FromListTargetToStr(rightExec, paramMode, paramDic))} {currentAlias} ON {currentOnStr}";

                var leftStr = JoinToStr(join.Left, toSql, replaceMembers, leftAlias, true, paramMode, paramDic);
                return (leftStr.sql + "\r\n" + right, true);
            }
            else if (item is ISqlFrom from)
            {
                var fromIt = FromListTargetToStr(from.Target, paramMode, paramDic);
                return ($"FROM {SubqueryParenthesis(fromIt)} {((fromIt.subQ || forceUpperAlias) ? upperAlias : "")}", false);
            }
            else if (item is ISqlFromListAlias alias)
            {
                return JoinToStr(alias.From, toSql, replaceMembers, upperAlias, forceUpperAlias, paramMode, paramDic);
            }

            throw new ArgumentException("El from-item debe de ser un JOIN, FROM o Alias()");
        }


    }
}
