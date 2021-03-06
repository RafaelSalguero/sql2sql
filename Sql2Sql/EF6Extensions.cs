﻿//using System;
//using System.Collections.Generic;
//using System.Data.Common;
//using System.Data.Entity;
//using System.Data.Entity.Infrastructure;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Npgsql;
//using Sql2Sql.Fluent;
//using NpgsqlTypes;

//namespace Sql2Sql
//{
//    /// <summary>
//    /// Extensiones de Entity Framework para Sql2Sql SQL
//    /// </summary>
//    public static class EF6Extensions
//    {
//        /// <summary>
//        /// Mapea un tipo de .NET a uno de Npgsql
//        /// Fuente: https://www.npgsql.org/doc/types/basic.html
//        /// </summary>
//        static Dictionary<Type, NpgsqlDbType> typeWriteMappings => new Dictionary<Type, NpgsqlDbType>
//        {
//            { typeof(bool), NpgsqlDbType.Boolean },
//            { typeof(short), NpgsqlDbType.Smallint },
//            { typeof(int), NpgsqlDbType.Integer },
//            { typeof(long), NpgsqlDbType.Bigint },
//            { typeof(float), NpgsqlDbType.Real },
//            { typeof(double), NpgsqlDbType.Double },
//            { typeof(decimal), NpgsqlDbType.Numeric },
//            { typeof(string), NpgsqlDbType.Text },
//            { typeof(char), NpgsqlDbType.Char },
//            { typeof(Guid), NpgsqlDbType.Uuid },
//            { typeof(DateTime), NpgsqlDbType.Date },
//            { typeof(TimeSpan), NpgsqlDbType.Interval},
//            { typeof(DateTimeOffset), NpgsqlDbType.TimestampTz},
//            { typeof(byte[]), NpgsqlDbType.Bytea},
//        };

//        static NpgsqlDbType MapParamType(Type t)
//        {
//            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
//            {
//                //Si el tipo es nullable, devolver el que no es nullable
//                return MapParamType(t.GetGenericArguments()[0]);
//            }
//            else if (t.IsEnum)
//            {
//                return MapParamType(t.GetEnumUnderlyingType());
//            }
//            else if (typeWriteMappings.TryGetValue(t, out var val))
//            {
//                return val;
//            }
//            throw new ArgumentException($"No se encontró el tipo '{t}' en los mapeos de tipos de parámetros de Npgsql");
//        }

//        static DbParameter[] getParams(IEnumerable<SqlParam> pars)
//        {
//            return pars.Select(x =>
//            {
//                var t = MapParamType(x.Type);
//                var p = new NpgsqlParameter(x.Name, t);
//                p.Value = x.Value ?? DBNull.Value;

//                return p;
//            }).ToArray();
//        }

//        /// <summary>
//        /// Ejecuta el query en un contexto de EF, las entidades devueltas no estan incluídas en el contexto
//        /// </summary>
//        public static DbRawSqlQuery<T> Execute<T, TDb>(this ISqlSelect<T> select, TDb context)
//         where TDb : DbContext
//        {
//            var sql = select.ToSql();
//            var pars = getParams(sql.Params);

//            return context.Database.SqlQuery<T>(sql.Sql, pars);
//        }

//        /// <summary>
//        /// Ejecuta un query que devuelve un conjunto del mismo tipo de un DbSet, las entidades devueltas si estan incluídas en el contexto
//        /// </summary>
//        public static DbSqlQuery<T> Execute<T>(this ISqlSelect<T> select, DbSet<T> set)
//            where T : class
//        {
//            var sql = select.ToSql();
//            var pars = getParams(sql.Params);

//            return set.SqlQuery(sql.Sql, pars);
//        }
//    }
//}
