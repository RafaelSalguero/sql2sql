using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SqlToSql.Fluent;
using SqlToSql.Fluent.Data;

namespace SqlToSql.SqlText
{
    public static class SqlSelect
    {
        static string OrderByItemStr(IOrderByExpr orderBy, SqlExprParams pars)
        {
            return
                $"{SqlExpression.ExprToSql(orderBy.Expr, pars.SetPars(orderBy.Expr.Parameters[0], null))} " +
                $"{(orderBy.Order == OrderByOrder.Asc ? "ASC" : orderBy.Order == OrderByOrder.Desc ? "DESC" : throw new ArgumentException())}" +
                $"{(orderBy.Nulls == OrderByNulls.NullsFirst ? " NULLS FIRST" : orderBy.Nulls == OrderByNulls.NullsLast ? " NULLS LAST" : "")}";

        }

        static string OrderByStr(IReadOnlyList<IOrderByExpr> orderBy, SqlExprParams pars)
        {
            if (orderBy == null || orderBy.Count == 0) return "";
            return $"ORDER BY {string.Join(", ", orderBy.Select(x => OrderByItemStr(x, pars)))}";
        }

        static string GroupByStr(IReadOnlyList<IGroupByExpr> groups, SqlExprParams pars)
        {
            if (groups == null || groups.Count == 0) return "";
            var exprs = string.Join(", ", groups.Select(x => SqlExpression.ExprToSql(x.Expr.Body, pars.SetPars(x.Expr.Parameters[0], null))));
            return $"GROUP BY {exprs}";
        }

        static string PartitionByStr(IReadOnlyList<IPartitionBy> groups, SqlExprParams pars)
        {
            if (groups == null || groups.Count == 0) return "";
            var exprs = string.Join(", ", groups.Select(x => SqlExpression.ExprToSql(x.Expr.Body, pars.SetPars(x.Expr.Parameters[0], null))));
            return $"PARTITION BY {exprs}";
        }

        static string WhereStr(LambdaExpression where, SqlExprParams p)
        {
            var pars = p.SetPars(where.Parameters[0], where.Parameters[1]);
            return $"WHERE {SqlExpression.ExprToSql(where.Body, pars)}";
        }

        public class NamedWindow
        {
            public NamedWindow(string name, ISqlWindowClause window)
            {
                Name = name;
                Window = window;
            }

            public string Name { get; }
            public ISqlWindowClause Window { get; }
        }

        static string WindowFrameClauseStr(SqlWinFrame frame)
        {
            if (frame == null) return "";
            var grouping =
                    frame.Grouping == WinFrameGrouping.Rows ? "ROWS" :
                    frame.Grouping == WinFrameGrouping.Range ? "RANGE" :
                    frame.Grouping == WinFrameGrouping.Groups ? "GROUPS" :
                    throw new ArgumentException();

            string startEnd(SqlWindowFrameStartEnd x) =>
                    x.Type == WinFrameStartEnd.UnboundedPreceding ? "UNBOUNDED PRECEDING" :
                    x.Type == WinFrameStartEnd.OffsetPreceding ? $"{x.Offset} PRECEDING" :
                    x.Type == WinFrameStartEnd.CurrentRow ? "CURRENT ROW" :
                    x.Type == WinFrameStartEnd.OffsetFollowing ? $"{x.Offset} FOLLOWING" :
                    x.Type == WinFrameStartEnd.UnboundedFollowing ? "UNBOUNDED FOLLOWING" :
                    throw new ArgumentException();

            var ret = grouping;
            ret +=
                frame.End != null ? $" BETWEEN {startEnd(frame.Start)} AND {startEnd(frame.End)}" :
                startEnd(frame.Start);

            if (frame.Exclusion != null)
            {
                var ex =
                    frame.Exclusion == WinFrameExclusion.CurrentRow ? "CURRENT ROW" :
                    frame.Exclusion == WinFrameExclusion.Group ? "GROUP" :
                    frame.Exclusion == WinFrameExclusion.Ties ? "TIES" :
                    frame.Exclusion == WinFrameExclusion.NoOthers ? "NO OTHERS" :
                    throw new ArgumentException();

                ret += " EXCLUDE " + ex;
            }
            return ret;
        }

        static string WindowDefToStr(ISqlWindowClause window, IEnumerable<NamedWindow> others, SqlExprParams pars)
        {
            var existingName = others.Where(x => x.Window == window.ExistingWindow).Select(x => x.Name).FirstOrDefault();
            if (existingName == null && window.ExistingWindow != null)
            {
                throw new ArgumentException("No se encontró el WINDOW existente");
            }

            return
$@"{(existingName ?? "")}
{PartitionByStr(window.PartitionBy, pars)}
{OrderByStr(window.OrderBy, pars)}
{WindowFrameClauseStr(window.Frame)}
";
        }

        static string WindowToStr(IWindowClauses windows, SqlExprParams pars)
        {
            var obj = windows.Windows;
            var props = obj.GetType().GetProperties().Select(x => new NamedWindow(x.Name, (x.GetValue(obj) as ISqlWindow)?.Current)).ToList();

            var noSonWin = props.Where(x => x.Window == null);
            if (noSonWin.Any())
            {
                throw new ArgumentException("Existen algunas definiciones de WINDOW incorrectas");
            }

            var ret = props.Select(x => $"WINDOW \"{x.Name}\" AS ({WindowDefToStr(x.Window, props, pars)})");
            return string.Join(", \r\n", ret);
        }


        /// <summary>
        /// Convierte la expresión de proyección de un SELECT a sql, devuelve si la proyección es escalar
        /// </summary>
        /// <param name="map"></param>
        /// <param name="pars"></param>
        /// <returns></returns>
        static (string sql, bool scalar) SelectStr(LambdaExpression map, SqlExprParams pars)
        {
            var param = map.Parameters[0];
            var body = map.Body;

            string MemberAssigToSql(Expression expr, MemberInfo prop)
            {
                var exprSql = SqlExpression.ExprToSqlStar(expr, pars);
                if (exprSql.star)
                {
                    return exprSql.sql;
                }
                return $"{exprSql.sql} AS \"{prop.Name}\"";
            }

            if (body is MemberInitExpression member)
            {
                return (string.Join(", ",
                        member.Bindings.Cast<MemberAssignment>()
                        .Select(x => MemberAssigToSql(x.Expression, x.Member))
                    ), false);
            }
            else if (body is NewExpression newExpr)
            {
                var typeProps = newExpr.Type.GetProperties().ToList();
                var consParams = newExpr.Constructor.GetParameters().Select(x => x.Name).ToList();

                return (string.Join(", ",
                        newExpr.Arguments.Select((arg, i) => MemberAssigToSql(arg, typeProps.First(x => x.Name.ToLower() == consParams[i].ToLower())))
                    ), false);
            }

            var bodySql = SqlExpression.ExprToSqlStar(body, pars);
            return (bodySql.sql, !bodySql.star);
        }

        /// <summary>
        /// Convierte una cláusula de SELECT a string
        /// </summary>
        public static string SelectToString(ISelectClause clause)
        {
            return SelectToStringScalar(clause).sql;
        }
       
        /// <summary>
        /// Convierte una cláusula de SELECT a string
        /// </summary>
        public static (string sql, bool scalar) SelectToStringScalar(ISelectClause clause)
        {
            var fromAlias = $"\"{clause.Select.Parameters[0].Name}\"";
            var from = SqlFromList.FromListToStr(clause.From, fromAlias, true);
            var selectParam = clause.Select.Parameters[0];
            var aliases = from.Aliases.ToList();
            if (!from.Named)
            {
                //Agregar el parametro del select como el nombre del fromList, esto para que se sustituya correctamente en los subqueries
                aliases.Add(new SqlFromList.ExprStrAlias(selectParam, fromAlias));
            }
            var pars = new SqlExprParams(selectParam, clause.Select.Parameters[1], from.Named, fromAlias,  aliases);
            var select = SelectStr(clause.Select, pars);

            var ret = new StringBuilder();
            ret.AppendLine($"SELECT {select.sql}");
            ret.AppendLine(from.Sql);
            if (clause.Where != null)
            {
                ret.AppendLine(WhereStr(clause.Where, pars));
            }
            if (clause.GroupBy != null)
            {
                ret.AppendLine(GroupByStr(clause.GroupBy, pars));
            }
            if (clause.OrderBy != null)
            {
                ret.AppendLine(OrderByStr(clause.OrderBy, pars));
            }
            if (clause.Window != null)
            {
                ret.AppendLine(WindowToStr(clause.Window, pars));
            }


            //Borra el ultimo salto de linea
            ret.Length = ret.Length - 2;
            return (ret.ToString(), select.scalar);
        }
    }
}
