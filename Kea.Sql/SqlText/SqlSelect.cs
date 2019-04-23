using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using KeaSql.Fluent;
using KeaSql.Fluent.Data;

namespace KeaSql.SqlText
{
    public static class SqlSelect
    {
        public static string TabStr(string s)
        {
            return string.Join("\r\n",
               s
                 .Split(new[] { "\r\n" }, StringSplitOptions.None)
                 .Select(x => "    " + x)
            );
        }

        public static string DetabStr(string s)
        {
            return
                string.Join("\r\n",
                s
                  .Trim(' ', '\t', '\r', '\n')
                 .Split(new[] { "\r\n" }, StringSplitOptions.None)
                 .Select(x => x.Trim(' ', '\t'))
                );
        }

        static string OrderByItemStr( IOrderByExpr orderBy, SqlExprParams pars)
        {
            return
                $"{SqlExpression.ExprToSql(orderBy.Expr.Body, pars.SetPars(orderBy.Expr.Parameters[0], null))} " +
                $"{(orderBy.Order == OrderByOrder.Asc ? "ASC" : orderBy.Order == OrderByOrder.Desc ? "DESC" : throw new ArgumentException())}" +
                $"{(orderBy.Nulls == OrderByNulls.NullsFirst ? " NULLS FIRST" : orderBy.Nulls == OrderByNulls.NullsLast ? " NULLS LAST" : "")}";

        }

        static string OrderByStr(IReadOnlyList<IOrderByExpr> orderBy, SqlExprParams pars)
        {
            return $"ORDER BY {string.Join(", ", orderBy.Select(x => OrderByItemStr(x, pars)))}";
        }

        static string GroupByStr(IReadOnlyList<IGroupByExpr> groups, SqlExprParams pars)
        {
            var exprs = string.Join(", ", groups.Select(x => SqlExpression.ExprToSql(x.Expr.Body, pars.SetPars(x.Expr.Parameters[0], null))));
            return $"GROUP BY {exprs}";
        }

        static string PartitionByStr(IReadOnlyList<IPartitionBy> groups, SqlExprParams pars)
        {
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

            List<string> retItems = new List<string>(); ;
            if (existingName != null)
            {
                retItems.Add(existingName);
            }
            if (window.PartitionBy.Any())
            {
                retItems.Add(PartitionByStr(window.PartitionBy, pars));
            }
            if (window.OrderBy.Any())
            {
                retItems.Add(OrderByStr(window.OrderBy, pars));
            }
            if (window.Frame != null)
            {
                retItems.Add(WindowFrameClauseStr(window.Frame));
            }
            return string.Join("\r\n", retItems);
        }

        static string WindowToStr(IWindowClauses windows, SqlExprParams pars)
        {
            var obj = windows.Windows;
            var props = obj.GetType().GetTypeInfo().DeclaredProperties.Select(x => new NamedWindow(x.Name, (x.GetValue(obj) as ISqlWindow)?.Current)).ToList();

            var noSonWin = props.Where(x => x.Window == null);
            if (noSonWin.Any())
            {
                throw new ArgumentException("Existen algunas definiciones de WINDOW incorrectas");
            }

            var ret = props.Select(x => $"\"{x.Name}\" AS (\r\n{TabStr(WindowDefToStr(x.Window, props, pars))}\r\n)");
            return "WINDOW \r\n" + TabStr(string.Join(", \r\n", ret));
        }


        /// <summary>
        /// Convierte la expresión de proyección de un SELECT a sql, devuelve si la proyección es escalar
        /// </summary>
        /// <param name="map"></param>
        /// <param name="pars"></param>
        /// <returns></returns>
        internal static (string sql, bool scalar) SelectStr( Expression body, SqlExprParams pars)
        {
            string MemberAssigToSql(Expression expr, MemberInfo prop)
            {
                var exprSql = SqlExpression.ExprToSqlStar( expr, pars);
                if (exprSql.star)
                {
                    return exprSql.sql;
                }
                return $"{exprSql.sql} AS \"{prop.Name}\"";
            }

            string pegarItems(IEnumerable<string> its) => string.Join(", \r\n", its);

            if (body is MemberInitExpression member)
            {
                return (pegarItems(
                        member.Bindings.Cast<MemberAssignment>()
                        .Select(x => MemberAssigToSql(x.Expression, x.Member))
                    ), false);
            }
            else if (body is NewExpression newExpr)
            {
                var typeProps = newExpr.Type.GetTypeInfo().DeclaredProperties.ToList();
                var consParams = newExpr.Constructor.GetParameters().Select(x => x.Name).ToList();

                return (pegarItems(
                        newExpr.Arguments.Select((arg, i) => MemberAssigToSql(arg, typeProps.First(x => x.Name.ToLower() == consParams[i].ToLower())))
                    ), false);
            }

            var bodySql = SqlExpression.ExprToSqlStar(body, pars);
            return (bodySql.sql, !bodySql.star);
        }

        /// <summary>
        /// Convierte una cláusula de SELECT a string, sin parámetros
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public static string SelectToStringSP(ISelectClause clause)
        {
            return SelectToString(clause, ParamMode.None, new SqlParamDic());
        }
        /// <summary>
        /// Convierte una cláusula de SELECT a string
        /// </summary>
        public static string SelectToString(ISelectClause clause, ParamMode paramMode, SqlParamDic paramDic)
        {
            return SelectToStringScalar(clause, paramMode, paramDic).sql;
        }


        /// <summary>
        /// Convierte una cláusula de SELECT a string
        /// </summary>
        public static (string sql, bool scalar) SelectToStringScalar(ISelectClause clause, ParamMode paramMode, SqlParamDic paramDic)
        {
            var fromAlias = $"\"{clause.Select.Parameters[0].Name}\"";
            var from = SqlFromList.FromListToStr(clause.From, fromAlias, true, paramMode, paramDic);
            var selectParam = clause.Select.Parameters[0];
            var aliases = from.Aliases.ToList();
            if (!from.Named)
            {
                //Agregar el parametro del select como el nombre del fromList, esto para que se sustituya correctamente en los subqueries
                aliases.Add(new SqlFromList.ExprStrAlias(selectParam, fromAlias));
            }
            var pars = new SqlExprParams(selectParam, clause.Select.Parameters[1], from.Named, fromAlias, aliases, paramMode, paramDic);
            var select = SelectStr(clause.Select.Body, pars);

            var ret = new StringBuilder();

            ret.AppendLine($"SELECT \r\n{TabStr(select.sql)}");
            ret.AppendLine(from.Sql);
            if (clause.Where != null)
            {
                ret.AppendLine(WhereStr(clause.Where, pars));
            }
            if (clause.GroupBy.Any())
            {
                ret.AppendLine(GroupByStr(clause.GroupBy, pars));
            }
            if (clause.OrderBy.Any())
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
