using Sql2Sql.Mapper.ILCtors;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Mapper.Ctors
{
    /// <summary>
    /// Constructor mapping logic
    /// </summary>
    static class MappingLogic
    {
        static int IndexOf<T>(this IEnumerable<T> items, Func<T, bool> pred) => items.Select((x, i) => (x, i)).Where(x => pred(x.x)).Select(X => X.i).First();

        /// <summary>
        /// Create a mapping between a type and a list of columns
        /// </summary>
        /// <param name="prefix">Only look into columns with this prefix</param>
        public static ValueMapping CreateMapping(Type type, string prefix, IReadOnlyList<string> columns)
        {
            if (PathAccessor.IsSimpleType(type))
                return new SingularMapping(type, columns.IndexOf(x => x.ToLowerInvariant() == prefix.ToLowerInvariant()));

            var constructors = type.GetConstructors();
            if (constructors.Length == 0)
                throw new ArgumentException($"The type '{type}' has no public constructors");

            //Select the constructor with the most number of arguments:
            var cons = constructors.OrderByDescending(x => x.GetParameters().Length).FirstOrDefault();
            var consMap = MapConstructor(cons, prefix + "_", columns);

            var props = type.GetProperties().Where(x => x.GetSetMethod() != null);
            var propMap = MapProperties(props, prefix + "_", columns);

            return new CtorMapping(cons, consMap, propMap);
        }

        /// <summary>
        /// Map object property setters
        /// </summary>
        static IReadOnlyDictionary<PropertyInfo, ValueMapping> MapProperties(IEnumerable<PropertyInfo> props, string prefix, IReadOnlyList<string> columns)
        {
            var maps = props
                .Select(x => new
                {
                    map = MapProperty(x.PropertyType, x.Name, prefix, columns),
                    prop = x
                })
                .ToDictionary(
                    x => x.prop,
                    x => x.map
                )
                ;
            return maps;
        }

        /// <summary>
        /// Map constructor arguments
        /// </summary>
        static IReadOnlyList<ValueMapping> MapConstructor(ConstructorInfo cons, string prefix, IReadOnlyList<string> columns)
        {
            var pars = cons.GetParameters();
            var type = cons.DeclaringType;

            var propMappings = pars
                .Select(x => MapProperty(x.ParameterType, x.Name, prefix, columns))
                .ToList()
                ;
            return propMappings;
        }

        /// <summary>
        /// Returns a value mapping for a given property
        /// </summary>
        /// <param name="prefix">Only look into columns with this prefix</param>
        /// <param name="columns"></param>
        static ValueMapping MapProperty(Type type, string property, string prefix, IReadOnlyList<string> columns)
        {
            return CreateMapping(type, prefix + property, columns);
        }

    }
}
