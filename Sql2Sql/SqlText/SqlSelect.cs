using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sql2Sql.Fluent;
using Sql2Sql.Fluent.Data;
using Sql2Sql.SqlText.Rewrite;

namespace Sql2Sql.SqlText
{
    /// <summary>
    /// Conversión de la cláusula de SQL a string
    /// </summary>
    static class SqlSelect
    {
        public static string TabStr(string s)
        {
            return string.Join(Environment.NewLine,
               s
                 .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                 .Select(x => "    " + x)
            );
        }

        public static string DetabStr(string s)
        {
            return
                string.Join(Environment.NewLine,
                s
                  .Trim(' ', '\t', '\r', '\n')
                 .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                 .Select(x => x.Trim(' ', '\t'))
                );
        }

        static string OrderByItemStr(OrderByExpr orderBy, SqlExprParams pars)
        {
            return
                $"{SqlExpression.ExprToSql(orderBy.Expr.Body, pars.ReplaceSelectParams(orderBy.Expr.Parameters[0], null), true)} " +
                $"{(orderBy.Order == OrderByOrder.Asc ? "ASC" : orderBy.Order == OrderByOrder.Desc ? "DESC" : throw new ArgumentException())}" +
                $"{(orderBy.Nulls == OrderByNulls.NullsFirst ? " NULLS FIRST" : orderBy.Nulls == OrderByNulls.NullsLast ? " NULLS LAST" : "")}";

        }

        static string OrderByStr(IReadOnlyList<OrderByExpr> orderBy, SqlExprParams pars)
        {
            return $"ORDER BY {string.Join(", ", orderBy.Select(x => OrderByItemStr(x, pars)))}";
        }


        static string DistinctOnStr(IReadOnlyList<LambdaExpression> expr, SqlExprParams pars)
        {
            return $"DISTINCT ON ({string.Join(", ", expr.Select(x => SqlExpression.ExprToSql(x.Body, pars.ReplaceSelectParams(x.Parameters[0], null), true)))})";
        }

        static string GroupByStr(IReadOnlyList<GroupByExpr> groups, SqlExprParams pars)
        {
            var exprs = string.Join(", ", groups.Select(x => SqlExpression.ExprToSql(x.Expr.Body, pars.ReplaceSelectParams(x.Expr.Parameters[0], null), true)));
            return $"GROUP BY {exprs}";
        }

        static string PartitionByStr(IReadOnlyList<PartitionByExpr> groups, SqlExprParams pars)
        {
            var exprs = string.Join(", ", groups.Select(x => SqlExpression.ExprToSql(x.Expr.Body, pars.ReplaceSelectParams(x.Expr.Parameters[0], null), true)));
            return $"PARTITION BY {exprs}";
        }

        static string WhereStr(LambdaExpression where, SqlExprParams p)
        {
            var pars = p.ReplaceSelectParams(where.Parameters[0], where.Parameters[1]);
            return $"WHERE {SqlExpression.ExprToSql(where.Body, pars, true)}";
        }

        public class NamedWindow
        {
            public NamedWindow(string name, SqlWindowClause window)
            {
                Name = name;
                Window = window;
            }

            public string Name { get; }
            public SqlWindowClause Window { get; }
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

        static string WindowDefToStr(SqlWindowClause window, IEnumerable<NamedWindow> others, SqlExprParams pars)
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
            return string.Join(Environment.NewLine, retItems);
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

            var ret = props.Select(x => $"\"{x.Name}\" AS ({Environment.NewLine}{TabStr(WindowDefToStr(x.Window, props, pars))}{Environment.NewLine})");
            return $"WINDOW {Environment.NewLine}" + TabStr(string.Join($", {Environment.NewLine}", ret));
        }

        /// <summary>
        /// Extrae las expresiones y los miembros que corresponden de una expresión ya sea <see cref="MemberInitExpression"/> o <see cref="NewExpression"/>
        /// </summary>
        public static IEnumerable<(Expression expr, MemberInfo mem)> ExtractInitExpr(Expression body)
        {
            if (body is MemberInitExpression member)
            {
                return member.Bindings.Cast<MemberAssignment>()
                        .Select(x => (x.Expression, x.Member))
                        ;

            }
            else if (body is NewExpression newExpr)
            {
                var typeProps = newExpr.Type.GetTypeInfo().DeclaredProperties.ToList();
                var consParams = newExpr.Constructor.GetParameters().Select(x => x.Name).ToList();

                return newExpr.Arguments.Select((arg, i) => (arg, (MemberInfo)typeProps.First(x => x.Name.ToLower() == consParams[i].ToLower())))
                    ;
            }
            throw new ArgumentException($"'{nameof(body)}' debe de ser tipo MemberInitExpression o NewExpression");
        }

        /// <summary>
        /// Obtiene el nombre de la columna o del SELECT "AS" dado un <see cref="MemberInfo"/> y el <see cref="SqlExpression.SqlSubpath"/> correspondiente
        /// </summary>
        public static string MemberToColumnName(MemberInfo member, SqlExpression.SqlSubpath subpath) => member.Name + subpath.Subpath;

        /// <summary>
        /// Convierte un nombre de una columna al SQL correspondiente, para posgres esto es sólo ponerle las comillas dobles.
        /// Puede ser * para indicar todas las columnas
        /// </summary>
        public static string ColNameToStr(string colName) => colName == "*" ? colName : $"\"{colName}\"";

        /// <summary>
        /// Convierte un nombre de una tabla al SQL correspondiente, para posgres esto es sólo ponerle las comillas dobles
        /// </summary>
        public static string TableNameToStr(string tabName) => $"\"{tabName}\"";

        /// <summary>
        /// Convierte la expresión de proyección de un SELECT a sql, devuelve si la proyección es escalar
        /// </summary>
        public static SelectExprToStrResult SelectBodyToStr(Expression body, SqlExprParams pars)
        {
            var visitor = new SqlRewriteVisitor(pars);
            body = visitor.Visit(body);

            IEnumerable<ValueCol> MemberAssigToSql(Expression expr, MemberInfo prop)
            {
                var exprSql = SqlExpression.ExprToSqlStar(expr, pars, false);
                if (exprSql.star)
                {
                    return new[] {
                         new ValueCol (SqlExpression.SqlSubpath.Single(exprSql.sql), null )
                        };
                }

                var asList = exprSql.sql.Select(subpath =>
                 new ValueCol(subpath.Sql, MemberToColumnName(prop, subpath))
                );

                return asList;
            }


            if (body is MemberInitExpression || body is NewExpression)
            {
                var exprs = ExtractInitExpr(body)
                    .SelectMany(x => MemberAssigToSql(x.expr, x.mem))
                    .ToList()
                    ;

                return new SelectExprToStrResult(exprs, false);
            }

            var bodySql = SqlExpression.ExprToSqlStar(body, pars, false);
            if (bodySql.sql.Count > 1)
            {
                throw new ArgumentException("Por ahora no esta permitido devolver un ComplexType como el resultado de un SELECT");
            }
            return new SelectExprToStrResult(
                new[] {
                   new ValueCol( bodySql.sql.First().Sql, null)
                },
                !bodySql.star);
        }

        /// <summary>
        /// Convierte el parseado de la expresión del Select a string 
        /// </summary>
        public static string SelectExprToStr(IEnumerable<ValueCol> values)
        {
            var lines =
                values.Select(x =>
                x.Value + (
                x.Column == null ? "" : (" AS " + ColNameToStr(x.Column))
                )
                );

            return string.Join($", {Environment.NewLine}", lines);
        }

        /// <summary>
        /// Convierte una cláusula de SELECT a string, sin parámetros
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public static string SelectToStringSP(SelectClause clause)
        {
            return SelectToString(clause, ParamMode.None, new SqlParamDic());
        }
        /// <summary>
        /// Convierte una cláusula de SELECT a string
        /// </summary>
        public static string SelectToString(SelectClause clause, ParamMode paramMode, SqlParamDic paramDic)
        {
            return SelectToStringScalar(clause, paramMode, paramDic).Sql;
        }


        /// <summary>
        /// Convierte una cláusula de SELECT a string
        /// </summary>
        public static SelectToStrResult SelectToStringScalar(SelectClause clause, ParamMode paramMode, SqlParamDic paramDic)
        {
            var paramName = clause.Select.Parameters[0].Name;
            var from = SqlFromList.FromListToStr(clause.From, paramName, true, paramMode, paramDic);
            var selectParam = clause.Select.Parameters[0];
            var aliases = from.Aliases.ToList();
            if (!from.Named)
            {
                //Agregar el parametro del select como el nombre del fromList, esto para que se sustituya correctamente en los subqueries
                aliases.Add(new SqlFromList.ExprStrRawSql(selectParam, TableNameToStr(from.Alias)));
            }
            var pars = new SqlExprParams(selectParam, clause.Select.Parameters[1], from.Named, from.Alias, aliases, paramMode, paramDic);

            var select = SelectBodyToStr(clause.Select.Body, pars);

            var ret = new StringBuilder();

            ret.Append("SELECT");
            if (clause.DistinctType != SelectType.All)
            {
                ret.Append(" ");
                switch (clause.DistinctType)
                {
                    case SelectType.Distinct:
                        ret.Append("DISTINCT");
                        break;
                    case SelectType.DistinctOn:
                        ret.Append(DistinctOnStr(clause.DistinctOn, pars));
                        break;
                }
            }
            ret.AppendLine();
            ret.AppendLine($"{TabStr(SelectExprToStr(select.Values))}");
            ret.AppendLine(from.Sql);
            if (clause.Where != null)
            {
                ret.AppendLine(WhereStr(clause.Where, pars));
            }
            if (clause.GroupBy?.Any() == true)
            {
                ret.AppendLine(GroupByStr(clause.GroupBy, pars));
            }
            if (clause.Window != null)
            {
                ret.AppendLine(WindowToStr(clause.Window, pars));
            }
            if (clause.OrderBy?.Any() == true)
            {
                ret.AppendLine(OrderByStr(clause.OrderBy, pars));
            }
            if (clause.Limit != null)
            {
                ret.AppendLine("LIMIT " + clause.Limit);
            }

            //Delete the last line jump, note that the lenght of the line-jump
            //depends on the operating system
            ret.Length = ret.Length - Environment.NewLine.Length;
            return new SelectToStrResult(ret.ToString(), select.Values.Select(x => x.Column).ToList(), select.Scalar);
        }
    }
}
