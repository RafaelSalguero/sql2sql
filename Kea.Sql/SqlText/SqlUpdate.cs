using KeaSql.SqlText.Rewrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.SqlText
{
    /// <summary>
    /// Convertir a SQL una cláusula de UPDATE
    /// </summary>
    static class SqlUpdate
    {
        /// <summary>
        /// Convierte el cuerpo de la expresión SET a SQL
        /// </summary>
        public static string SetToSql(Expression body,
            ParamMode paramMode,
            SqlParamDic paramDic,
            IEnumerable<SqlFromList.ExprStrRawSql> exprAlias
            )
        {
            var b = new StringBuilder();
            var pars = new SqlExprParams(null, null, false, null, exprAlias.ToList(), paramMode, paramDic);

            //Hacer el rewrite en todo el body:
            var visitor = new SqlRewriteVisitor(pars);
            body = visitor.Visit(body);

            var exprs = SqlSelect
            .ExtractInitExpr(body)
            .Select(x => (x.mem, sql: SqlExpression.ExprToSqlStar(x.expr, pars, false)))
            ;

            if (exprs.Any(y => y.sql.star))
                throw new ArgumentException("No esta soportado una expresión star '*' en la asignación de los valores de un INSERT");

            var subpaths = exprs.SelectMany(x => x.sql.sql, (parent, child) => (member: parent.mem, subpath: child));
            var sets = subpaths.Select(x => (
                column: SqlSelect.MemberToColumnName(x.member, x.subpath),
                value: x.subpath.Sql))
                ;

            var setSql = sets
                .Select(x =>
                    $"{SqlSelect.ColNameToStr(x.column)} = {x.value}"
                );

            var sql = string.Join(", \r\n", setSql);
            return sql;
        }
    }
}
