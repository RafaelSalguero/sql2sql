using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Sql2Sql.Mapper
{
    /// <summary>
    /// Clase que convierte un object a cierto tipo. La instancia guarda un cache que hace mucho más rápidas las conversiones
    /// posteriores del mismo tipo
    /// </summary>
    public class ExprCast
    {
        static Delegate CreateConvertDelegate(Type destType, Type sourceType)
        {
            var param = Expression.Parameter(typeof(object), "data");
            var body = Expression.Block(Expression.Convert(Expression.Convert(param, sourceType), destType));

            var ret = Expression.Lambda(body, param).Compile();
            return ret;
        }

        readonly ConcurrentDictionary<(Type dest, Type source), Delegate> convertDels = new ConcurrentDictionary<(Type dest, Type source), Delegate>();

        Delegate GetConvertDelegate(Type destType, Type sourceType)
        {
            var key = (destType, sourceType);
            return convertDels.GetOrAdd(key, k => CreateConvertDelegate(k.dest, k.source));
        }

        /// <summary>
        /// Convierte un object a cierto tipo
        /// </summary>
        public object Cast(Type destType, object data)
        {
            if (data == null) return null;
            var del = GetConvertDelegate(destType, data.GetType());
            return del.DynamicInvoke(new object[] { data });
        }
    }
}
