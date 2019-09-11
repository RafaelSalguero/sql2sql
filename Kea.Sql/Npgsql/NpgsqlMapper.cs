using System.Collections.Generic;
using System.Threading.Tasks;
using Kea.Mapper;
using KeaSql;
using KeaSql.Npgsql;
using Npgsql;

namespace KeaSql.Npgsql
{
    public static class NpgsqlMapper
    {
        /// <summary>
        /// Agrega los parámetros a un <see cref="NpgsqlCommand"/>
        /// </summary>
        static void AddParams(NpgsqlCommand cmd, IEnumerable<SqlParam> sqlPars)
        {
            var pars = NpgsqlExtensions.GetParams(sqlPars);
            foreach (var p in pars)
            {
                cmd.Parameters.Add(p);
            }
        }

        /// <summary>
        /// Ejecuta un query en un NpgsqlConnection
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
        /// Ejecuta un statement en un NpgsqlConnection, devuelve el número de filas afectadas
        /// </summary>
        public static async Task<int> Execute<T>(NpgsqlConnection conn, SqlResult sql)
        {
            using (var cmd = new NpgsqlCommand(sql.Sql, conn))
            {
                AddParams(cmd, sql.Params);

                return await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
