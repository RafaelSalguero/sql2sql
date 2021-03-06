﻿using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sql2Sql.Npgsql
{
    /// <summary>
    /// Extensions methods for executing Sql2Sql queries in a given <see cref="NpgsqlConnection"/>
    /// </summary>
    public static class NpgsqlExtensions
    {
        /// <summary>
        /// Wrap the query in a LIMIT 1 query and returns the result. Retrurns null if the result is empty
        /// </summary>
        public static async Task<T> FirstOrDefaultAsync<T>(this ISqlSelect<T> select, NpgsqlConnection conn)
        {
            var q = Sql.From(select).Limit(1);
            var r = await q.ToListAsync(conn);
            return r.FirstOrDefault();
        }

        /// <summary>
        /// Wrap the query in a LIMIT 1 query and returns the result. Throws if the result is empty
        /// </summary>
        public static async Task<T> FirstAsync<T>(this ISqlSelect<T> select, NpgsqlConnection conn)
        {
            var q = Sql.From(select).Limit(1);
            var r = await q.ToListAsync(conn);
            return r.First();
        }

        /// <summary>
        /// Wrap the query in a LIMIT 1 query and returns the result. Retrurns null if the result is empty
        /// </summary>
        public static T FirstOrDefault<T>(this ISqlSelect<T> select, NpgsqlConnection conn)
        {
            var q = Sql.From(select).Limit(1);
            var r = q.ToList(conn);
            return r.FirstOrDefault();
        }

        /// <summary>
        /// Wrap the query in a LIMIT 1 query and returns the result. Throws if the result is empty
        /// </summary>
        public static T First<T>(this ISqlSelect<T> select, NpgsqlConnection conn)
        {
            var q = Sql.From(select).Limit(1);
            var r = q.ToList(conn);
            return r.First();
        }

        /// <summary>
        /// Wrap the query in a LIMIT 2 query and returns the result. Returns null if there are not exactly one element in the result
        /// </summary>
        public static async Task<T> SingleOrDefaultAsync<T>(this ISqlSelect<T> select, NpgsqlConnection conn)
        {
            var q = Sql.From(select).Limit(2);
            var r = await q.ToListAsync(conn);
            return r.SingleOrDefault();
        }

        /// <summary>
        /// Wrap the query in a LIMIT 2 query and returns the result. Returns null if there are not exactly one element in the result
        /// </summary>
        public static T SingleOrDefault<T>(this ISqlSelect<T> select, NpgsqlConnection conn)
        {
            var q = Sql.From(select).Limit(2);
            var r =  q.ToList(conn);
            return r.SingleOrDefault();
        }


        /// <summary>
        /// Wrap the query in a LIMIT 2 query and returns the result. Throws if there is not exactly one element 
        /// </summary>
        public static async Task<T> SingleAsync<T>(this ISqlSelect<T> select, NpgsqlConnection conn)
        {
            var q = Sql.From(select).Limit(2);
            var r = await q.ToListAsync(conn);
            return r.Single();
        }

        /// <summary>
        /// Wrap the query in a LIMIT 2 query and returns the result. Throws if there is not exactly one element 
        /// </summary>
        public static T Single<T>(this ISqlSelect<T> select, NpgsqlConnection conn)
        {
            var q = Sql.From(select).Limit(2);
            var r = q.ToList(conn);
            return r.Single();
        }

        /// <summary>
        /// Wrap the query in a COUNT(1) query and returns the result
        /// </summary>
        public static async Task<int> CountAsync<T>(this ISqlSelect<T> select, NpgsqlConnection conn)
        {
            return (int)(await select.LongCountAsync(conn));
        }


        /// <summary>
        /// Wrap the query in a COUNT(1) query and returns the result
        /// </summary>
        public static int Count<T>(this ISqlSelect<T> select, NpgsqlConnection conn)
        {
            return (int)select.LongCount(conn);
        }

        /// <summary>
        /// Wrap the query in a COUNT(1) query and returns the result
        /// </summary>
        public static async Task<long> LongCountAsync<T>(this ISqlSelect<T> select, NpgsqlConnection conn)
        {
            var q = Sql.From(select).Select(x => Sql.Count(1));
            return await q.FirstAsync(conn);
        }

        /// <summary>
        /// Wrap the query in a COUNT(1) query and returns the result
        /// </summary>
        public static long LongCount<T>(this ISqlSelect<T> select, NpgsqlConnection conn)
        {
            var q = Sql.From(select).Select(x => Sql.Count(1));
            return q.First(conn);
        }

        /// <summary>
        /// Execute the given query and returns the result as a read only list
        /// </summary>
        public static async Task<IReadOnlyList<T>> ToListAsync<T>(this ISqlQuery<T> select, NpgsqlConnection conn)
        {
            var sql = select.ToSql();
            var pars = NpgsqlParamLogic.GetParams(sql.Params);
            return await NpgsqlMapper.QueryAsync<T>(conn, sql);
        }

        /// <summary>
        /// Execute the given query and returns the result as a read only list
        /// </summary>
        public static IReadOnlyList<T> ToList<T>(this ISqlQuery<T> select, NpgsqlConnection conn)
        {
            var sql = select.ToSql();
            var pars = NpgsqlParamLogic.GetParams(sql.Params);
            return NpgsqlMapper.Query<T>(conn, sql);
        }

        /// <summary>
        /// Execute the given statement and returns the number of affected rows
        /// </summary>
        public static async Task<int> ExecuteAsync<TDb>(this ISqlStatement statement, NpgsqlConnection conn)
        {
            var sql = statement.ToSql();
            var pars = NpgsqlParamLogic.GetParams(sql.Params);
            return await NpgsqlMapper.ExecuteAsync(conn, sql);
        }

        /// <summary>
        /// Execute the given statement and returns the number of affected rows
        /// </summary>
        public static int Execute<TDb>(this ISqlStatement statement, NpgsqlConnection conn)
        {
            var sql = statement.ToSql();
            var pars = NpgsqlParamLogic.GetParams(sql.Params);
            return NpgsqlMapper.Execute(conn, sql);
        }

    }
}
