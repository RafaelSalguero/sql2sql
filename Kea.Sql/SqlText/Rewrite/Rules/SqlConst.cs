﻿using System;
using System.Linq;
using System.Reflection;
using KeaSql.ExprRewrite;
using KeaSql.Fluent;

namespace KeaSql.SqlText.Rewrite.Rules
{
    public class SqlConst
    {
        /// <summary>
        /// Convierte una constante a SQL
        /// </summary>
        public static string ConstToSql<T>(T value)
        {
            if (value == null)
            {
                return "NULL";
            }
            var type = value.GetType();
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var val = ((dynamic)value).Value;
                return ConstToSql(val);
            }
            else if (typeInfo.IsEnum)
            {
                var member = typeInfo.DeclaredMembers.Where(x => x.Name == value.ToString()).FirstOrDefault();
                if (member != null && (member.GetCustomAttribute<SqlNameAttribute>() is var att) && att != null)
                {
                    return att.SqlName;
                }
                return ((int)(object)value).ToString();
            }

            if (value is string || value is Guid)
            {
                return $"'{value}'";
            }
            else if (
                value is decimal || value is int || value is float || value is double || value is long || value is byte || value is sbyte ||
                value is bool
                )
            {
                return value.ToString();
            }
            else if ((object)value is DateTime date)
            {
                if (date.Date - date == TimeSpan.Zero)
                {
                    //No tiene componente de horas
                    return $"'{date.ToString("yyyy-MM-dd")}'";
                }
                else
                {
                    return $"'{date.ToString("yyyy-MM-dd HH:mm:ss")}'";
                }
            }
            else if ((object)value is DateTimeOffset dateOff)
            {
                var off = dateOff.Offset;
                var timeZoneOffset = (off < TimeSpan.Zero ? "-" : "+") + off.ToString("hh:mm");

                if (dateOff.LocalDateTime.Date - dateOff.LocalDateTime == TimeSpan.Zero)
                {
                    return $"'{dateOff.ToString("yyyy-MM-dd")} {timeZoneOffset}'";
                }
                else
                {
                    return $"'{dateOff.ToString("yyyy-MM-dd HH:mm:ss")} {timeZoneOffset}'";
                }
            }
            throw new ArgumentException($"No se puede convertir a SQL la constante " + value.ToString());
        }

        public static readonly RewriteRule constToSqlRule = RewriteRule.Create(
            (RewriteTypes.C1 x) => RewriteSpecial.Constant(x),
            x => Sql.Raw<RewriteTypes.C1>(ConstToSql(x)));
    }
}
