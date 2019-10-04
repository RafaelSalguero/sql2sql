using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Fluent.Data
{
    /// <summary>
    /// Set members in immutable classes
    /// </summary>
    internal static class Immutable
    {
        /// <summary>
        /// Extracts the property name from an x => x.Prop expression
        /// </summary>
        static PropertyInfo ExtractProperty(LambdaExpression propExpr)
        {
            if (propExpr.Body is MemberExpression mem)
            {
                return (PropertyInfo)mem.Member;
            }
            throw new ArgumentException();
        }

        /// Return a new instance of <typeparamref name="T"/> with the specified property assigned to a list with newValue added to the end of the list.
        /// If the property initial value was null, a list with a single element is created
        public static T Add<T, TProp>(T instance, Expression<Func<T, IReadOnlyList<TProp>>> property, TProp newValue)
        {
            var propToSet = ExtractProperty(property);
            var oldList = (IReadOnlyList<TProp>)propToSet.GetValue(instance) ?? new TProp[0];
            var nextList = (IReadOnlyList<TProp>)oldList.Concat(new[] { newValue }).ToList();
            return Set(instance, property, nextList);
        }
        /// <summary>
        /// Return a new instance of <typeparamref name="T"/> with the specified property assigned to the given value
        /// </summary>
        public static T Set<T, TProp>(T instance, Expression<Func<T, TProp>> property, TProp newValue)
        {
            var propToSet = ExtractProperty(property);
            return Set(instance, propToSet, newValue);
        }

        /// <summary>
        /// Return a new instance of <typeparamref name="T"/> with the specified property assigned to the given value
        /// </summary>
        static T Set<T, TProp>(T instance, PropertyInfo propToSet, TProp newValue)
        {
            var cons = typeof(T).GetConstructors().Single();
            var props = typeof(T).GetProperties();

            var pars = cons.GetParameters();
            var parProps =
                from pa in pars
                join pr in props on pa.Name.ToLowerInvariant() equals pr.Name.ToLowerInvariant()
                select new
                {
                    param = pa,
                    prop = pr
                };

            var parVals = parProps.Select(
                x =>
                    x.prop == propToSet ? newValue :
                    x.prop.GetValue(instance)
                ).ToArray();

            return (T)cons.Invoke(parVals);
        }
    }
}

