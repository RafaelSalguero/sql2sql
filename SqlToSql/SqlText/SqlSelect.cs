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

        static string SelectExpressionStr(Expression ex, Expression param)
        {
            if (ex is MemberExpression member && member.Expression == param)
            {
                return $"\"{member.Member.Name}\"";
            }
            return null;
        }

        static string OrderByItemStr(IOrderByExpr orderBy)
        {
            return
                $"{SelectExpressionStr(orderBy.Expr.Body, orderBy.Expr.Parameters[0])} " +
                $"{(orderBy.Order == OrderByOrder.Asc ? "ASC" : orderBy.Order == OrderByOrder.Desc ? "DESC" : throw new ArgumentException())}" +
                $"{(orderBy.Nulls == OrderByNulls.NullsFirst ? " NULLS FIRST" : orderBy.Nulls == OrderByNulls.NullsLast ? " NULLS LAST" : "")}";

        }

        static string OrderByStr(IReadOnlyList<IOrderByExpr> orderBy)
        {
            if (orderBy == null || orderBy.Count == 0) return "";
            return $"ORDER BY {string.Join(", ", orderBy.Select(OrderByItemStr))}";
        }

        static string GroupByStr(IReadOnlyList<IGroupByExpr> groups)
        {
            if (groups == null || groups.Count == 0) return "";
            var exprs = string.Join(", ", groups.Select(x => SelectExpressionStr(x.Expr.Body, x.Expr.Parameters[0])));
            return $"GROUP BY {exprs}";
        }

        static string PartitionByStr(IReadOnlyList<IPartitionBy> groups)
        {
            if (groups == null || groups.Count == 0) return "";
            var exprs = string.Join(", ", groups.Select(x => SelectExpressionStr(x.Expr.Body, x.Expr.Parameters[0])));
            return $"PARTITION BY {exprs}";
        }

        static string WhereStr(LambdaExpression where)
        {
            var param = where.Parameters[0];
            return $"WHERE {SelectExpressionStr(where.Body, param)}";
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

        static string WindowDefToStr(ISqlWindowClause window, IEnumerable<NamedWindow> others)
        {
            var existingName = others.Where(x => x.Window == window.ExistingWindow).Select(x => x.Name).FirstOrDefault();
            if (existingName == null && window.ExistingWindow != null)
            {
                throw new ArgumentException("No se encontró el WINDOW existente");
            }

            return
$@"{(existingName ?? "")}
{PartitionByStr(window.PartitionBy)}
{OrderByStr(window.OrderBy)}
{WindowFrameClauseStr(window.Frame)}
";
        }

        static string WindowToStr(IWindowClauses windows)
        {
            var obj = windows.Windows;
            var props = obj.GetType().GetProperties().Select(x => new NamedWindow(x.Name, (x.GetValue(obj) as ISqlWindow)?.Current)).ToList();

            var noSonWin = props.Where(x => x.Window == null);
            if (noSonWin.Any())
            {
                throw new ArgumentException("Existen algunas definiciones de WINDOW incorrectas");
            }

            var ret = props.Select(x => $"WINDOW \"{x.Name}\" AS ({WindowDefToStr(x.Window, props)})");
            return string.Join(", \r\n", ret);
        }


        static string SelectStr(LambdaExpression map, bool fromListNamed, string fromlistAlias)
        {
            var param = map.Parameters[0];
            var body = map.Body;
            string SubStr(Expression ex) => SelectExpressionStr(ex, param);
            string ToSql(Expression ex) => SqlExpression.ExprToSql(ex, SubStr);

            string MemberAssigToSql(Expression expr, MemberInfo prop)
            {
                if (expr == param)
                {
                    return $"{fromlistAlias}.*";
                }
                if (fromListNamed && expr is MemberExpression mem && mem.Expression == param)
                {
                    return $"{ToSql(mem)}.*";
                }

                if (fromListNamed)
                {
                    return $"{ToSql(expr)} AS \"{prop.Name}\"";
                }
                return $"{fromlistAlias}.{ToSql(expr)} AS \"{prop.Name}\"";
            }

            if (body is MemberInitExpression member)
            {
                return string.Join(", ",
                        member.Bindings.Cast<MemberAssignment>()
                        .Select(x => MemberAssigToSql(x.Expression, x.Member))
                    );
            }
            else if (body is NewExpression newExpr)
            {
                var typeProps = newExpr.Type.GetProperties().ToList();
                var consParams = newExpr.Constructor.GetParameters().Select(x => x.Name).ToList();

                return string.Join(", ",
                        newExpr.Arguments.Select((arg, i) => MemberAssigToSql(arg, typeProps.First(x => x.Name.ToLower() == consParams[i].ToLower())))
                    );
            }
            else if (body == param)
            {
                return $"{fromlistAlias}.*";
            }

            return SelectExpressionStr(body, param);
        }

        /// <summary>
        /// Convierte una cláusula de SELECT a string
        /// </summary>
        public static string SelectToString(ISelectClause clause)
        {
            var fromAlias = $"\"{clause.Select.Parameters[0].Name}\"";
            var from = SqlFromList.FromListToStr(clause.From, fromAlias);
            var select = SelectStr(clause.Select, from.Named, fromAlias);

            var ret = new StringBuilder();
            ret.AppendLine($"SELECT {select}");
            ret.AppendLine(from.Sql);
            if (clause.Where != null)
            {
                ret.AppendLine(WhereStr(clause.Where));
            }
            if (clause.GroupBy != null)
            {
                ret.AppendLine(GroupByStr(clause.GroupBy));
            }
            if (clause.OrderBy != null)
            {
                ret.AppendLine(OrderByStr(clause.OrderBy));
            }
            if(clause.Window != null)
            {
                ret.AppendLine(WindowToStr(clause.Window));
            }

            return ret.ToString();
        }
    }
}
