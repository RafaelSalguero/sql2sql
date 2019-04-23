using System;
using System.Collections.Generic;
using System.Linq;
using KeaSql;
using Npgsql;
using NpgsqlTypes;

namespace KeaSql.Npgsql
{
    public static class NpgsqlExtensions
    {
        /// <summary>
        /// Mapea un tipo de .NET a uno de Npgsql
        /// Fuente: https://www.npgsql.org/doc/types/basic.html
        /// </summary>
        static Dictionary<Type, NpgsqlDbType> typeWriteMappings => new Dictionary<Type, NpgsqlDbType>
                {
                    { typeof(bool), NpgsqlDbType.Boolean },
                    { typeof(short), NpgsqlDbType.Smallint },
                    { typeof(int), NpgsqlDbType.Integer },
                    { typeof(long), NpgsqlDbType.Bigint },
                    { typeof(float), NpgsqlDbType.Real },
                    { typeof(double), NpgsqlDbType.Double },
                    { typeof(decimal), NpgsqlDbType.Numeric },
                    { typeof(string), NpgsqlDbType.Text },
                    { typeof(char), NpgsqlDbType.Char },
                    { typeof(Guid), NpgsqlDbType.Uuid },
                    { typeof(DateTime), NpgsqlDbType.Date },
                    { typeof(TimeSpan), NpgsqlDbType.Interval},
                    { typeof(DateTimeOffset), NpgsqlDbType.TimestampTz},
                    { typeof(byte[]), NpgsqlDbType.Bytea},
                };

        /// <summary>
        /// Mapea un tipo de .NET a uno de Npgsql
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static NpgsqlDbType MapParamType(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                //Si el tipo es nullable, devolver el que no es nullable
                return MapParamType(t.GetGenericArguments()[0]);
            }
            else if (t.IsEnum)
            {
                return MapParamType(t.GetEnumUnderlyingType());
            }
            else if (typeWriteMappings.TryGetValue(t, out var val))
            {
                return val;
            }
            throw new ArgumentException($"No se encontró el tipo '{t}' en los mapeos de tipos de parámetros de Npgsql");
        }

        /// <summary>
        /// Convierte un conjunto de <see cref="SqlParam"/> a un arreglo de <see cref="NpgsqlParameter"/>
        /// </summary>
        /// <param name="pars"></param>
        /// <returns></returns>
        public static NpgsqlParameter[] GetParams(IEnumerable<SqlParam> pars)
        {
            return pars.Select(x =>
            {
                var t = MapParamType(x.Type);
                var p = new NpgsqlParameter(x.Name, t);
                p.Value = x.Value ?? DBNull.Value;

                return p;
            }).ToArray();
        }
    }
}
