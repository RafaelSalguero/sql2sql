using Npgsql;
using Sql2Sql.Mapper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Npgsql
{
    /// <summary>
    /// Execute queries in Npgsql
    /// </summary>
    public static class NpgsqlMapper
    {
        /// <summary>
        /// Add the given parameters to an <see cref="NpgsqlCommand"/>
        /// </summary>
        static void AddParams(NpgsqlCommand cmd, IEnumerable<SqlParam> sqlPars)
        {
            var pars = NpgsqlParamLogic.GetParams(sqlPars);
            foreach (var p in pars)
            {
                cmd.Parameters.Add(p);
            }
        }

        /// <summary>
        /// Execute a query and returns the result
        /// </summary>
        public static async Task<IReadOnlyList<T>> Query<T>(NpgsqlConnection conn, SqlResult sql)
        {
            using (var cmd = new NpgsqlCommand(sql.Sql, conn))
            {
                AddParams(cmd, sql.Params);

                //Ejecutar el query:
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    return await DbReader.ReadAsync<T>(reader, ColumnMatchMode.Source);
                }
            }
        }

        /// <summary>
        /// Execute an statement and return the number of affected rows
        /// </summary>
        public static async Task<int> Execute(NpgsqlConnection conn, SqlResult sql)
        {
            using (var cmd = new NpgsqlCommand(sql.Sql, conn))
            {
                AddParams(cmd, sql.Params);
                return await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
