using System;
using System.Linq;
using KeaSql;
using KeaSql.SqlText;
using Microsoft.EntityFrameworkCore;

namespace KeaSql.EFCore
{
    public static class EFCoreExtensions
    {
        /// <summary>
        /// Convierte un Select de Kea.Sql a un IQueryable de EFCore, relacionado con cierto DbSet
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
            where T: class
            where TDbSet : DbSet<T>
        {
            return select.ExecuteIQueryable(set);
        }

        /// <summary>
        /// Convierte un Select de Kea.Sql a un query de EFCore, donde el tipo del query no necesariamente es un DbSet
        /// </summary>
        public static IQueryable<T> ToIQueryable<T>(this ISqlSelect<T> select, DbContext context)
            where T : class
        {
            return select.ExecuteIQueryable(context.Query<T>()).AsNoTracking();
        }
    }
}
