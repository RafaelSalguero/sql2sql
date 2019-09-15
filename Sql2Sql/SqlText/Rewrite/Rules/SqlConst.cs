using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Sql2Sql.ExprRewrite;
using Sql2Sql.Fluent;

namespace Sql2Sql.SqlText.Rewrite.Rules
{
    public class SqlConst
    {
        public static string ConstToSql<T>(T value)
        {
            return ConstToSqlObj(value);
        }
        /// <summary>
        /// Convierte una constante a SQL
        /// </summary>
        public static string ConstToSqlObj(object value)
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

        static bool CanBeConst(object val)
        {
            if (val == null)
                return true;
            if (val is Expression)
                return false;
            if (val.GetType().Name.Contains("<"))
                return false;
            if (val is string)
                return true;
            if (val is IEnumerable)
                return false;

            return true;
        }
        public static readonly RewriteRule constToSqlRule = RewriteRule.Create(
                 "constToSql",
                 (RewriteTypes.C1 x) => RewriteSpecial.Constant(x),
                 x => RewriteSpecial.Atom(Sql.Raw<RewriteTypes.C1>(ConstToSql(x))),
                 (match, expr) => CanBeConst(((ConstantExpression)match.Args[0]).Value)
                 );

    }
}
