using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeaSql;
using KeaSql.Npgsql;
using KeaSql.SqlText;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KeaSql.EFCore
{
    public static class EFCoreExtensions
    {
        class CountResult
        {
            public int Resultado { get; set; }
        }

        /// <summary>
        /// Obtiene el primer resultado del query
        /// </summary>
        public static async Task<T> FirstOrDefaultAsync<T, TDb>(this ISqlSelect<T> select, TDb context)
            where TDb : DbContext
        {
            var q = Sql.From(select).Limit(1);
            var r = await q.ToListAsync(context);
            return r.FirstOrDefault();
        }

        /// <summary>
        /// Obtiene el primer resultado del query
        /// </summary>
        public static async Task<T> FirstAsync<T, TDb>(this ISqlSelect<T> select, TDb context)
            where TDb : DbContext
        {
            var q = Sql.From(select).Limit(1);
            var r = await q.ToListAsync(context);
            return r.First();
        }

        /// <summary>
        /// Obtiene el primer resultado del query
        /// </summary>
        public static async Task<T> SingleOrDefaultAsync<T, TDb>(this ISqlSelect<T> select, TDb context)
            where TDb : DbContext
        {
            var q = Sql.From(select).Limit(2);
            var r = await q.ToListAsync(context);
            return r.SingleOrDefault();
        }


        /// <summary>
        /// Obtiene el primer resultado del query
        /// </summary>
        public static async Task<T> SingleAsync<T, TDb>(this ISqlSelect<T> select, TDb context)
            where TDb : DbContext
        {
            var q = Sql.From(select).Limit(2);
            var r = await q.ToListAsync(context);
            return r.Single();
        }
        /// <summary>
        /// Ejecuta un query que cuenta la cantidad de elementos en el select
        /// </summary>
        public static async Task<int> CountAsync<T, TDb>(this ISqlSelect<T> select, TDb context)
            where TDb : DbContext
        {
            return (int)(await select.LongCountAsync(context));
        }


        /// <summary>
        /// Ejecuta un query que cuenta la cantidad de elementos en el select
        /// </summary>
        public static async Task<long> LongCountAsync<T, TDb>(this ISqlSelect<T> select, TDb context)
            where TDb : DbContext
        {
            var q = Sql.From(select).Select(x => Sql.Count(1));
            return await q.SingleAsync(context);
        }

        /// <summary>
        /// Ejecuta el query en un contexto de EF, las entidades devueltas no estan incluídas en el contexto
        /// </summary>
        public static async Task<IReadOnlyList<T>> ToListAsync<T, TDb>(this ISqlQuery<T> select, TDb context)
            where TDb : DbContext
        {
            var sql = select.ToSql();
            var pars = NpgsqlExtensions.GetParams(sql.Params);
            return await DoConnection(context, async conn => await NpgsqlMapper.Query<T>(conn, sql));
        }

        /// <summary>
        /// Ejecuta el query en un contexto de EF. Devuelve la cantidad de filas afectadas
        /// </summary>
        public static async Task<int> Execute<TDb>(this ISqlStatement statement, TDb context)
            where TDb : DbContext
        {
            var sql = statement.ToSql();
            var pars = NpgsqlExtensions.GetParams(sql.Params);
            return await DoConnection(context, async conn => await NpgsqlMapper.Execute(conn, sql));
        }

        /// <summary>
        /// Ejecuta un conjunto de queries en un contexto de EF
        /// </summary>
        public static async Task Execute<TDb>(this IEnumerable<ISqlStatement> statements, TDb context)
        {

        }

        /// <summary>
        /// Ejecuta una acción en función de una conexión de Npgsql, esta conexión se obtiene a partir de un DbContext
        /// </summary>
        static async Task<T> DoConnection<T, TDb>(TDb context, Func<NpgsqlConnection, Task<T>> action)
            where TDb : DbContext
        {
            var conn = context.Database.GetDbConnection() as NpgsqlConnection;

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
                return await action(conn);
            }
            finally
            {
                if (cerrarConn)
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// Convierte un Select de Kea.Sql a un IQueryable de EFCore, relacionado con cierto DbSet o query type
        /// </summary>
        static IQueryable<T> ExecuteIQueryable<T>(this ISqlSelect<T> select, IQueryable<T> set)
            where T : class
        {
            var sql = select.ToSql(ParamMode.EntityFramework);
            var pars = KeaSql.Npgsql.NpgsqlExtensions.GetParams(sql.Params);
            return set.FromSql(sql.Sql, pars);
        }

        /// <summary>
        /// Convierte un Select de Kea.Sql a un IQueryable de EFCore, relacionado con cierto DbSet
        /// </summary>
        public static IQueryable<T> ToIQueryableSet<T, TDbSet>(this ISqlSelect<T> select, TDbSet set)
            where T : class
            where TDbSet : DbSet<T>
        {
            return select.ExecuteIQueryable(set);
        }

        /// <summary>
        /// Convierte un Select de Kea.Sql a un query de EFCore, donde el tipo del query no necesariamente es un DbSet pero sai debe de estar
        /// registrado en el model como un query type
        /// </summary>
        public static IQueryable<T> ToIQueryable<T>(this ISqlSelect<T> select, DbContext context)
            where T : class
        {
            return select.ExecuteIQueryable(context.Query<T>()).AsNoTracking();
        }
    }
}
