﻿using System.Collections.Generic;
using System.Data.Entity;
using System.Threading.Tasks;
using Sql2Sql.Npgsql;
using Sql2Sql;
using Sql2Sql.Npgsql;
using Npgsql;

namespace Sql2Sql.EF6
{
    /// <summary>
    /// Extensiones de Entity Framework para Sql2Sql
    /// </summary>
    public static class EF6Extensions
    {
        /// <summary>
        /// Ejecuta el query en un contexto de EF, las entidades devueltas no estan incluídas en el contexto
        /// </summary>
        public static async Task<IReadOnlyList<T>> ToListAsync<T, TDb>(this ISqlSelect<T> select, TDb context)
            where TDb : DbContext
            where T : class, new()
        {
            var sql = select.ToSql();
            var pars = NpgsqlExtensions.GetParams(sql.Params);
            var conn = context.Database.Connection as NpgsqlConnection;

            var cerrarConn = false;
            if (conn.State == System.Data.ConnectionState.Closed)
            {
                //Si la conexión estaba cerrada, la abrimos para ejecutar el query pero luego la volvemos a cerrar,
                //esto para dejar el estado tal y como estaba antes de ejecutar el ToListAsync
                cerrarConn = true;
                await conn.OpenAsync();
            }

            try
            {
                return await NpgsqlMapper.Query<T>(conn, sql);
            }
            finally
            {
                if (cerrarConn)
                {
                    conn.Close();
                }
            }
        }


    }
}