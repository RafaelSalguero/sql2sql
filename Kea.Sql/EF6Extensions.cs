﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using KeaSql.Fluent;

namespace KeaSql
{
    /// <summary>
    /// Extensiones de Entity Framework para kea SQL
    /// </summary>
    public static class EF6Extensions
    {
        static DbParameter[] getParams(IEnumerable<SqlParam> pars)
        {
            return pars.Select(x => new NpgsqlParameter(x.Name, x.Value)).ToArray();
        }

        /// <summary>
        /// Ejecuta el query en un contexto de EF, las entidades devueltas no estan incluídas en el contexto
        /// </summary>
        public static DbRawSqlQuery<T> Execute<T, TDb>(this ISqlSelect<T> select, TDb context)
         where TDb : DbContext
        {
            var sql = select.ToSql();
            var pars = getParams(sql.Params);

            return context.Database.SqlQuery<T>(sql.Sql, pars);
        }

        /// <summary>
        /// Ejecuta un query que devuelve un conjunto del mismo tipo de un DbSet, las entidades devueltas si estan incluídas en el contexto
        /// </summary>
        public static DbSqlQuery<T> Execute<T>(this ISqlSelect<T> select, DbSet<T> set)
            where T : class
        {
            var sql = select.ToSql();
            var pars = getParams(sql.Params);

            return set.SqlQuery(sql.Sql, pars);
        }
    }
}