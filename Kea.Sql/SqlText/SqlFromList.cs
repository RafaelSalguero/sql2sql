using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using KeaSql.ExprRewrite;
using KeaSql.ExprTree;
using KeaSql.Fluent;
using KeaSql.SqlText.Rewrite;
using static KeaSql.ExprTree.ExprReplace;

namespace KeaSql.SqlText
{
    /// <summary>
    /// Convierte un FROM-LIST a SQL
    /// </summary>
    public static class SqlFromList
    {

        
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
                    find: leftOnParam == null ? x.Expr : ReplaceVisitor.Replace(x.Expr, leftParam, leftOnParam),
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

        /// <summary>
        /// Resultado de convertir un FROM list a SQL
        /// </summary>
        public class FromListToStrResult
        {
            public FromListToStrResult(string sql, bool named, string alias, IReadOnlyList<ExprStrRawSql> aliases)
            {
                Sql = sql;
                Named = named;
                Alias = alias;
                Aliases = aliases;
            }

            /// <summary>
            /// SQL generado del FROM list
            /// </summary>
            public string Sql { get; }

            /// <summary>
            /// Si el from list tiene aliases
            /// </summary>
            public bool Named { get; }

            /// <summary>
            /// El alias del FROM list, sólo aplica si <see cref="Named"/> = false, en otro caso será null.
            /// Note que este no es SQL así que no tiene las comillas
            /// </summary>
            public string Alias { get; }

            /// <summary>
            /// Aliases de las expresiones del from
            /// </summary>
            public IReadOnlyList<ExprStrRawSql> Aliases { get; }
        }

        /// <summary>
        /// Agrega los parentesis si subQ es true
        /// </summary>
        static string SubqueryParenthesis(StatementToStrResult fromList)
        {
            switch (fromList)
            {
                case QueryToStrResult _:
                    return $"(\r\n{SqlSelect.TabStr(fromList.Sql)}\r\n)";
                case TableToStrResult _:
                    return fromList.Sql;
                default:
                    throw new ArgumentException();
            }
        }


        /// <summary>
        /// Indica un alias de SQL para una expresión, de tal manera que al encontrar esta expresión se va a sustituir por el SQL Raw
        /// </summary>
        public class ExprStrRawSql
        {
            public ExprStrRawSql(Expression expr, string alias)
            {
                Expr = expr;
                Sql = alias;
            }

            /// <summary>
            /// Expresión a buscar
            /// </summary>
            public Expression Expr { get; }

            /// <summary>
            /// SQL por el cual se va a sustituir la expresión
            /// </summary>
            public string Sql { get; }
        }

        /// <summary>
        /// Devuelve la cadena a la que corresponde la expresión o null en caso de que esta expresión no tenga ningún alias
        /// </summary>
        public static string ReplaceStringAliasMembers(Expression ex, IEnumerable<ExprStrRawSql> alias)
        {
            return alias.Where(x => CompareExpr.ExprEquals(x.Expr, ex)).Select(x => x.Sql).FirstOrDefault();
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
        /// <param name="paramName">El nombre del parámetro del SELECT, en caso de que el FROM list no tenga alias, este será el alias del from list</param>
        /// <returns></returns>
        public static FromListToStrResult FromListToStr(IFromListItem item, string paramName, bool forceUpperAlias, ParamMode paramMode, SqlParamDic paramDic)
        {
            var alias = ExtractJoinAliases(item).SelectMany(x => x).Select(x => new ExprStrRawSql(x.Find, x.Alias)).ToList();

            var pars = new SqlExprParams(null, null, false, null, alias, paramMode, paramDic);
            Func<Expression, string> toSql = ex => SqlExpression.ExprToSql(ex, pars, true);

            var join = JoinToStr(item, toSql, alias, paramName, forceUpperAlias, paramMode, paramDic);
            return new FromListToStrResult(join.sql, join.named, !join.named ? paramName : null, alias);
        }

        static Expression RawSqlTableExpr(Type rawType, string sql)
        {
            var method = typeof(Sql).GetTypeInfo().DeclaredMethods.Where(x => x.Name == nameof(Sql.RawRowRef) && x.IsGenericMethod).Single();
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
        public static Expression ReplaceSubqueryBody(Expression body, IEnumerable<ExprStrRawSql> replaceMembers)
        {
            //Sustituir con el replace members, con un RawSql
            Func<Expression, Expression> replaceRaw = (ex) =>
            {
                var sql = ReplaceStringAliasMembers(ex, replaceMembers);
                if (sql == null) return ex;
                var ret = RawSqlTableExpr(ex.Type, sql);
                return ret;
            };
            var bodyRaw = ReplaceVisitor.Replace(body, replaceRaw);
            return bodyRaw;
        }

        /// <summary>
        /// Reemplaza las referencias a las tablas de los join anteriores en un LATERAL subquery con RawSQL, devuelve el body reemplazado
        /// </summary>
        public static Expression ReplaceSubqueryLambda(LambdaExpression subquery, Expression leftParam, IEnumerable<ExprStrRawSql> replaceMembers)
        {
            var body = subquery.Body;
            var lateralParam = subquery.Parameters[0];

            //Reemplazar el parametro del lateral con el leftParam:
            var bodyLeft = ReplaceVisitor.Replace(body, lateralParam, leftParam);

            return ReplaceSubqueryBody(bodyLeft, replaceMembers);
        }

        static (string sql, bool named) JoinToStr(IFromListItem item, Func<Expression, string> toSql, IReadOnlyList<ExprStrRawSql> replaceMembers, string paramName, bool forceUpperAlias, ParamMode paramMode, SqlParamDic paramDic)
        {
            var paramAlias = SqlSelect.TableNameToStr(paramName);
            return JoinToStrAlias(item, toSql, replaceMembers, paramAlias, forceUpperAlias, paramMode, paramDic);
        }

        /// <summary>
        /// Convierte una lista de FROM a SQL.
        /// Devuelve si la lista de FROMs tiene aliases
        /// </summary>
        /// <param name="item">Elemento que representa ya sea a un FROM o a una lista de JOINS</param>
        /// <param name="toSql">Convierte una expresión a SQL</param>
        /// <param name="paramSql">Alias del parámetro en SQL</param>
        /// <returns></returns>
        static (string sql, bool named) JoinToStrAlias(IFromListItem item, Func<Expression, string> toSql, IReadOnlyList<ExprStrRawSql> replaceMembers, string paramSql, bool forceUpperAlias, ParamMode paramMode, SqlParamDic paramDic)
        {
            if (item is ISqlJoin join)
            {
                var currentAlias = toSql(join.Map.Parameters[1]);
                var currentOnStr = toSql(join.On.Body);
                var leftParam = join.Map.Parameters[0];
                var leftAlias = ReplaceStringAliasMembers(leftParam, replaceMembers);
                var rightSubsNoRep = ReplaceSubqueryLambda(join.Right, leftParam, replaceMembers);

                //Visitar el lado derecho del JOIN:
                //TODO: Hacer una función para visitar a las expresiones de lado derecho del JOIN

                var rightSubs = SqlRewriteVisitor.VisitFromItem(rightSubsNoRep);

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

                var right = $"{typeStr}JOIN {latStr}{SubqueryParenthesis(StatementStr.StatementToString(rightExec, paramMode, paramDic))} {currentAlias} ON {currentOnStr}";

                var leftStr = JoinToStrAlias(join.Left, toSql, replaceMembers, leftAlias, true, paramMode, paramDic);
                return (leftStr.sql + "\r\n" + right, true);
            }
            else if (item is ISqlFrom from)
            {
                var fromIt = StatementStr.StatementToString(from.Target, paramMode, paramDic);
                return ($"FROM {SubqueryParenthesis(fromIt)} {(((fromIt is QueryToStrResult) || forceUpperAlias) ? paramSql : "")}", false);
            }
            else if (item is ISqlFromListAlias alias)
            {
                return JoinToStrAlias(alias.From, toSql, replaceMembers, paramSql, forceUpperAlias, paramMode, paramDic);
            }

            throw new ArgumentException("El from-item debe de ser un JOIN, FROM o Alias()");
        }


    }
}
