using Sql2Sql.Fluent.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.SqlText.Delete
{
    /// <summary>
    /// Converts DELETE clauses to SQL
    /// </summary>
    static class SqlDeleteConverter
    {
        static void ConvertWhere(StringBuilder b, DeleteClause clause, ParamMode paramMode, SqlParamDic paramDic)
        {
            var whereParms = clause.Where.Parameters;
            var tableParam = whereParms[0];
            var usingParam = whereParms[1];

            var replace = new SqlFromList.ExprStrRawSql[0];
            var pars = new SqlExprParams(tableParam, null, false, null, replace, paramMode, paramDic);

            var whereSql = SqlExpression.ExprToSql(clause.Where.Body, pars, true);
            b.Append("WHERE ");
            b.Append(whereSql);
        }

        static void ConvertDelete(StringBuilder b, DeleteClause clause, ParamMode paramMode, SqlParamDic paramDic)
        {
            b.Append("DELETE FROM ");
            if (clause.Only)
                b.Append("ONLY ");

            b.Append(SqlSelect.TableNameToStr(clause.Table));

            if (clause.Using.Any())
            {
                b.AppendLine();
                b.Append("USING ");
                b.Append(string.Join(", ", clause.Using));
            }

            if (clause.Where != null)
            {
                b.AppendLine();
                ConvertWhere(b, clause, paramMode, paramDic);
            }
        }
    }
}
