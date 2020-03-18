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
        static int? SingleOrDefaultIndex<T>(this IEnumerable<T> items, Func<T, bool> pred) => items
            .Select((x, i) => (x, i))
            .Where(x => pred(x.x))
            .Select(X => (int?)X.i)
            .SingleOrDefault();

        static IReadOnlyList<string> GetColumns(IDataRecord record)
        {
            return Enumerable.Range(0, record.FieldCount).Select(x => record.GetName(x)).ToList();
        }

        /// <summary>
        /// Create a mapping between a type and a list of columns
        /// </summary>
        public static ValueMapping CreateMapping(Type type, IDataRecord record)
        {
            return CreateMapping(type, "", GetColumns(record));
        }

        /// <summary>
        /// Create a mapping between a type and a list of columns.
        /// </summary>
        /// <param name="prefix">Only look into columns with this prefix</param>
        public static ValueMapping CreateMapping(Type type, string prefix, IReadOnlyList<string> columns)
        {
            if (PathAccessor.IsSimpleType(type))
            {
                var ix = columns.SingleOrDefaultIndex(x => x.ToLowerInvariant().StartsWith(prefix.ToLowerInvariant()));
                if (ix == null)
                    return new NullMapping(type);

                return new SingularMapping(type, ix.Value);
            }

            var constructors = type.GetConstructors();
            if (constructors.Length == 0)
                throw new ArgumentException($"The type '{type}' has no public constructors");

            //Selects the first constructor with the most number of arguments that have a non-null mapping
            var (cons, consMap) = PickConstructor(constructors, prefix, columns);
            if (consMap == null)
            {
                return new NullMapping(type);
            }

            var props = type.GetProperties().Where(x => x.GetSetMethod() != null);
            var propMap = MapProperties(props, prefix, columns);

            return new CtorMapping(cons, consMap, propMap);
        }

        /// <summary>
        /// Gets the first constructor with the most number of arguments and a non-null mapping between all parameters and columns
        /// </summary>
        static (ConstructorInfo, IReadOnlyList<ValueMapping>) PickConstructor(IEnumerable<ConstructorInfo> constructors, string prefix, IReadOnlyList<string> columns)
          {
            var cons = constructors
                .OrderByDescending(x => x.GetParameters().Length)
                .Select(x => new
                {
                    cons = x,
                    mapping = MapConstructor(x, prefix, columns)
                })
                .Where(x => x.mapping != null)
                .FirstOrDefault();

            return (cons.cons, cons.mapping);
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
                .Where(x => x.map.Columns.Any())
                .ToDictionary(
                    x => x.prop,
                    x => x.map
                )
                ;
            return maps;
        }

        /// <summary>
        /// Map constructor arguments. Returns null if no succesful mapping is done
        /// </summary>
        static IReadOnlyList<ValueMapping> MapConstructor(ConstructorInfo cons, string prefix, IReadOnlyList<string> columns)
        {
            var pars = cons.GetParameters();
            var type = cons.DeclaringType;

            var propMappings = pars
                .Select(x => MapProperty(x.ParameterType, x.Name, prefix, columns))
                .ToList()
                ;

            var nullMappings = propMappings
                .Select((x, i) => new
                {
                    param = pars[i],
                    map = x
                })
                .Where(x => !x.map.Columns.Any())
                .Select(x => x.param.Name);

            if (nullMappings.Any())
            {
                return null;
            }
            return propMappings;
        }

        /// <summary>
        /// Returns a value mapping for a given property
        /// </summary>
        /// <param name="prefix">Only look into columns with this prefix</param>
        /// <param name="columns"></param>
        static ValueMapping MapProperty(Type type, string property, string prefix, IReadOnlyList<string> columns)
        {
            return CreateMapping(type, prefix == "" ? property : ($"{prefix}_{property}"), columns);
        }

    }
}
