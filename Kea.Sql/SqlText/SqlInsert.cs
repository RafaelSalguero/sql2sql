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
            var pars = new SqlExprParams(null, null, false, "", new SqlFromList.ExprStrAlias[0], paramMode, paramDic);

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
            b.AppendLine(")");

            return b.ToString();
        }

        /// <summary>
        /// Convierte una cláusula de INSERT a string
        /// </summary>
        public static string InsertToString(IInsertClause clause, ParamMode paramMode, SqlParamDic paramDic)
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
                var sql = SqlSelect.SelectToStringScalar(clause.Query, paramMode, paramDic);

                //Texto de las columnas:
                b.Append("(");
                b.Append(string.Join(", ", sql.Columns));
                b.AppendLine(")");

                b.Append(sql.Sql);
            }

            return b.ToString();
        }
    }
}
