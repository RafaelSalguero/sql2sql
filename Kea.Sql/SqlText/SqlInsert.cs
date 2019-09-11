using KeaSql.Fluent.Data;
using KeaSql.SqlText.Rewrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.SqlText
{
    /// <summary>
    /// Conversión de la cláusula de SQL INSERT a string
    /// </summary>
    static class SqlInsert
    {

        /// <summary>
        /// Convierte a string la parte del VALUE de un INSERT
        /// </summary>
        static string InsertValueToString(IInsertClause clause, ParamMode paramMode, SqlParamDic paramDic)
        {
            var b = new StringBuilder();
            //Note que aquí el fromAlias no afecta ya se se usan directamente los nombres de las columnas
            //y no se puede referenciar a la tabla de origen en los VALUES
            var pars = new SqlExprParams(null, null, false, null, new SqlFromList.ExprStrRawSql[0], paramMode, paramDic);

            //Hacer el rewrite en todo el body:
            var visitor = new SqlRewriteVisitor(pars);
            var body = visitor.Visit(clause.Value);

            var exprs = SqlSelect
                .ExtractInitExpr(body)
                .Select(x => (x.mem, sql: SqlExpression.ExprToSqlStar(x.expr, pars, false)))
                ;

            if (exprs.Any(y => y.sql.star))
                throw new ArgumentException("No esta soportado una expresión star '*' en la asignación de los valores de un INSERT");

            var subpaths = exprs.SelectMany(x => x.sql.sql, (parent, child) => (member: parent.mem, subpath: child));
            //Nombres de las columnas del INSERT
            var columns = subpaths
                .Select(x => SqlSelect.MemberToColumnName(x.member, x.subpath))
                .Select(SqlSelect.ColNameToStr);
            ;
            //Valores:
            var values = subpaths.Select(x => x.subpath.Sql);

            //Texto de las columnas:
            b.Append("(");
            b.Append(string.Join(", ", columns));
            b.AppendLine(")");

            //Texto de los vaues:
            b.Append("VALUES (");
            b.Append(string.Join(", ", values));
            b.Append(")");

            return b.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        static string OnConflictDoUpdate(OnConflictDoUpdateClause doUpdate, ParamMode paramMode, SqlParamDic paramDic, string origTableName)
        {
            var b = new StringBuilder();

            b.AppendLine("DO UPDATE");
            b.AppendLine("SET");
            var exprAlias = new[]
            {
                 new SqlFromList.ExprStrRawSql(doUpdate.Set.Parameters[0], "EXCLUDED"),
                 new SqlFromList.ExprStrRawSql(doUpdate.Set.Parameters[1], $"\"{origTableName}\""),
            };
            var setSql = SqlUpdate.SetToSql(doUpdate.Set.Body, paramMode, paramDic, exprAlias);
            b.Append(SqlSelect.TabStr(setSql));

            if (doUpdate.Where != null)
            {
                b.AppendLine();

                var pars = new SqlExprParams(null, null, false, "", new SqlFromList.ExprStrRawSql[0], paramMode, paramDic);
                var whereSql = SqlExpression.ExprToSql(doUpdate.Where.Body, pars, true);
                b.Append(whereSql);
            }

            return b.ToString();
        }

        /// <summary>
        /// Convierte la cláusura ON CONFLICT
        /// </summary>
        static string OnConflict(OnConflictClause onConf, ParamMode paramMode, SqlParamDic paramDic, string tableName)
        {
            var b = new StringBuilder();
            //fromAlias es null ya que en la expresion de indice de ON CONFLICT no se permiten
            //los identificadores de las tablas, sólo se permiten los nombres tal cual de las columnas
            var pars = new SqlExprParams(null, null, false, null, new SqlFromList.ExprStrRawSql[0], paramMode, paramDic);
            var indexExpr = onConf
                .IndexExpressions
                .Select(x => SqlExpression.ExprToSql(x.Body, pars.ReplaceSelectParams(x.Parameters[0], null), true))
                ;

            b.Append("ON CONFLICT ");
            if (indexExpr.Any())
            {
                b.Append("(");
                b.Append(string.Join(", ", indexExpr));
                b.Append(") ");
            }

            if (onConf.Where != null)
            {
                var whereSql = SqlExpression.ExprToSql(onConf.Where.Body, pars.ReplaceSelectParams(onConf.Where.Parameters[0], null), true);
                b.Append("WHERE ");
                b.Append(whereSql);
            }

            if (onConf.DoUpdate == null)
            {
                //Si DoUpdate es null se considera que es DO NOTHING
                b.Append("DO NOTHING");
            }
            else
            {
                b.Append(OnConflictDoUpdate(onConf.DoUpdate, paramMode, paramDic, tableName));
            }

            return b.ToString();
        }

        /// <summary>
        /// Convierte la cláusula RETURNING a SQL
        /// </summary>
        static (string sql, IReadOnlyList<string> cols) ReturningToString(LambdaExpression returning, ParamMode paramMode, SqlParamDic paramDic, string tableName)
        {
            var pars = new SqlExprParams(returning.Parameters[0], null, false, tableName, new SqlFromList.ExprStrRawSql[0], paramMode, paramDic);
            var select = SqlSelect.SelectBodyToStr(returning.Body, pars);
            var sql = $"RETURNING \r\n{SqlSelect.TabStr(SqlSelect.SelectExprToStr(select.Values))}";
            var cols = select.Values.Select(x => x.Column).ToList();

            return (sql, cols);
        }

        /// <summary>
        /// Convierte una cláusula de INSERT a string.
        /// Si el INSERT devuelve valores.
        /// 
        /// Devuelve ya sea un <see cref="InsertNoReturningStrResult"/> o un <see cref="InsertReturningToStr"/>
        /// </summary>
        public static StatementToStrResult InsertToString(IInsertClause clause, ParamMode paramMode, SqlParamDic paramDic)
        {
            var b = new StringBuilder();
            b.Append("INSERT INTO ");
            b.Append($"\"{clause.Table}\" ");

            if ((clause.Value == null) == (clause.Query == null))
                throw new ArgumentException("Query debe de ser null si value no es null");

            if (clause.Value != null)
            {
                b.Append(InsertValueToString(clause, paramMode, paramDic));
            }
            else
            {
                //Query
                var sqlQuery = SqlSelect.SelectToStringScalar(clause.Query, paramMode, paramDic);

                //Texto de las columnas:
                b.Append("(");
                b.Append(string.Join(", ", sqlQuery.Columns));
                b.AppendLine(")");

                b.Append(sqlQuery.Sql);
            }

            if (clause.OnConflict != null)
            {
                b.AppendLine();
                b.Append(OnConflict(clause.OnConflict, paramMode, paramDic, clause.Table));
            }

            //Columnas del returning
            IReadOnlyList<string> columns = null;
            if (clause.Returning != null)
            {
                b.AppendLine();
                var returning = ReturningToString(clause.Returning, paramMode, paramDic, clause.Table);
                b.Append(returning.sql);
                columns = returning.cols;
            }

            var sql = b.ToString();
            if (columns != null)
            {
                return new InsertReturningToStr(sql, columns);
            }
            return new InsertNoReturningStrResult(sql);
        }
    }
}
