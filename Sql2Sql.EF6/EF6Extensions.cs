using System.Collections.Generic;
using System.Data.Entity;
using System.Threading.Tasks;
using Sql2Sql.Npgsql;
using Sql2Sql;
using Npgsql;
using System;

namespace Sql2Sql.EF6
{
    /// <summary>
    /// Execute Sql2Sql queries in a <see cref="DbContext"/>
    /// </summary>
    public static class EF6Extensions
    {

        /// <summary>
        /// Wrap the query in a LIMIT 1 query and returns the result. Retrurns null if the result is empty
        /// </summary>
        public static async Task<T> FirstOrDefaultAsync<T, TDb>(this ISqlSelect<T> select, DbContext context)
        {
            return await DoConnection(context, conn => select.FirstOrDefaultAsync(conn));
        }

        /// <summary>
        /// Wrap the query in a LIMIT 1 query and returns the result. Throws if the result is empty
        /// </summary>
        public static async Task<T> FirstAsync<T, TDb>(this ISqlSelect<T> select, DbContext context)
        {
            return await DoConnection(context, conn => select.FirstAsync(conn));
        }

        /// <summary>
        /// Wrap the query in a LIMIT 1 query and returns the result. Returns null if there are not exactly one element in the result
        /// </summary>
        public static async Task<T> SingleOrDefaultAsync<T, TDb>(this ISqlSelect<T> select, DbContext context)
        {
            return await DoConnection(context, conn => select.SingleOrDefaultAsync(conn));
        }


        /// <summary>
        /// Wrap the query in a LIMIT 1 query and returns the result. Throws if there is not exactly one element 
        /// </summary>
        public static async Task<T> SingleAsync<T, TDb>(this ISqlSelect<T> select, DbContext context)
        {
            return await DoConnection(context, conn => select.SingleAsync(conn));
        }
        /// <summary>
        /// Wrap the query in a COUNT(1) query and returns the result
        /// </summary>
        public static async Task<int> CountAsync<T, TDb>(this ISqlSelect<T> select, DbContext context)
        {
            return await DoConnection(context, conn => select.CountAsync(conn));
        }


        /// <summary>
        /// Wrap the query in a COUNT(1) query and returns the result
        /// </summary>
        public static async Task<long> LongCountAsync<T, TDb>(this ISqlSelect<T> select, DbContext context)
        {
            return await DoConnection(context, conn => select.LongCountAsync(conn));
        }

        /// <summary>
        /// Execute the given query and returns the result as a read only list
        /// </summary>
        public static async Task<IReadOnlyList<T>> ToListAsync<T, TDb>(this ISqlQuery<T> select, DbContext context)
        {
            return await DoConnection(context, conn => select.ToListAsync(conn));
        }

        /// <summary>
        /// Ejecuta el query en un contexto de EF. Devuelve la cantidad de filas afectadas
        /// Abre la conexión en caso de que no este abierta y la deja abierta
        /// </summary>
        public static async Task<int> Execute<TDb>(this ISqlStatement statement, DbContext context)
        {
            var sql = statement.ToSql();
            var pars = NpgsqlParamLogic.GetParams(sql.Params);
            return await DoConnection(context, async conn => await NpgsqlMapper.Execute(conn, sql));
        }

        /// <summary>
        /// Execute an action inside a given context.
        /// If the underlying <see cref="NpgsqlConnection"/> is not open, opens it
        /// The connection is not closed afterwise
        /// </summary>
        static async Task<T> DoConnection<T>(DbContext context, Func<NpgsqlConnection, Task<T>> action)
        {
            //Extract the connection from the context:
            var conn = context.Database.Connection as NpgsqlConnection;

            if (conn.State == System.Data.ConnectionState.Closed)
            {
                //If the connection was closed, we open it
                //The connection should be leaved open in order to execute multiple queries on the
                //same context
                await conn.OpenAsync();
            }

            return await action(conn);
        }




    }
}