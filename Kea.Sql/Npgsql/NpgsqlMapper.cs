using System.Collections.Generic;
using System.Threading.Tasks;
using KeaSql;
using KeaSql.Npgsql;
using Npgsql;

namespace KeaSql.Npgsql
{
    public static class NpgsqlMapper
    {
        /// <summary>
        /// Ejecuta un query en un NpgsqlConnection
        /// </summary>
        public static async Task<IReadOnlyList<T>> Query<T>(NpgsqlConnection conn, SqlResult sql)
        {
            using (var cmd = new NpgsqlCommand(sql.Sql, conn))
            {
                //Agrega los parámetros al query:
                var pars = NpgsqlExtensions.GetParams(sql.Params);
                foreach (var p in pars)
                {
                    cmd.Parameters.Add(p);
                }

                //Ejecutar el query:
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var ret = new List<T>();
                    var mapper = new DbMapper<T>(reader);
                    while (await reader.ReadAsync())
                    {
                        var item = mapper.ReadCurrent<T>();
                        ret.Add(item);
                    }
                    return ret;
                }
            }
        }
    }
}
