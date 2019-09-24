using Sql2Sql.Fluent;
using Sql2Sql.SqlText.Insert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.SqlText
{
    /// <summary>
    /// Se encarga de convertir las sentencias de alto nive a string
    /// </summary>
    public static class StatementStr
    {
        /// <summary>
        /// Convierte una sentencia de SQL a string.
        /// Puede convertir SELECT (fluent o RAW), INSERT y TABLE
        /// </summary>
        public static StatementToStrResult StatementToString(ISqlStatement item, ParamMode paramMode, SqlParamDic paramDic)
        {
            switch (item)
            {
                case ISqlInsert insert:
                    return InsertToStr(insert, paramMode, paramDic);
                case ISqlQuery query:
                    return QueryToStr(query, paramMode, paramDic);
                case IFromListItemTarget fromItem:
                    return FromListTargetToStr(fromItem, paramMode, paramDic);
                default:
                    throw new ArgumentException(nameof(item));
            }
        }

        /// <summary>
        /// Convierte un INSERT a string
        /// </summary>
        static StatementToStrResult InsertToStr(ISqlInsert item, ParamMode paramMode, SqlParamDic paramDic)
        {
            if (item is ISqlInsertHasClause clau)
            {
                return SqlInsertConverter.InsertToString(clau.Clause, paramMode, paramDic);
            }
            throw new ArgumentException(nameof(item));
        }

        /// <summary>
        /// Convierte un <see cref="ISqlQuery"/> a string
        /// </summary>
        static QueryToStrResult QueryToStr(ISqlQuery item, ParamMode paramMode, SqlParamDic paramDic)
        {
            if (item is ISqlSelectHasClause select)
            {
                var str = SqlSelect.SelectToStringScalar(select.Clause, paramMode, paramDic);
                return new QueryColsToStrResult(str.Sql, str.Columns);
            }
            else if (item is ISqlWithSelect withSelect)
            {
                var withSql = SqlWith.WithToSql(withSelect.With.With, withSelect.With.Param, paramMode, paramDic);
                var subquerySql = FromListTargetToStr(withSelect.Query, paramMode, paramDic);
                var ret = $"{withSql}{Environment.NewLine}{subquerySql.Sql}";

                if (subquerySql is QueryColsToStrResult subCols)
                    return new QueryColsToStrResult(ret, subCols.Columns);

                return new QueryToStrResult(ret);

            }
            else if (item is ISqlSelectRaw subq)
            {
                return new QueryToStrResult(SqlSelect.DetabStr(subq.Raw));
            }
            throw new ArgumentException("El from item target debe de ser una tabla o un select");
        }

        /// <summary>
        /// Convierte un <see cref="SqlTable"/> a string 
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        static string TableToString(SqlTable table) => $"\"{table.Name}\"";

        /// <summary>
        /// Convierte un <see cref="IFromListItemTarget"/> a string, devuelve true si el elemento requiered de un alias
        /// </summary>
        static StatementToStrResult FromListTargetToStr(IFromListItemTarget item, ParamMode paramMode, SqlParamDic paramDic)
        {
            if (item is ISqlQuery query)
            {
                return QueryToStr(query, paramMode, paramDic);
            }
            else if (item is SqlTable table)
            {
                return new TableToStrResult(TableToString(table));
            }
            else if (item is ISqlTableRefRaw raw)
            {
                return new TableToStrResult(raw.Raw);
            }
            throw new ArgumentException("El from item target debe de ser una tabla o un select");
        }

    }
}
