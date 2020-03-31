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


        static IReadOnlyList<string> GetColumns(IDataRecord record)
        {
            return Enumerable.Range(0, record.FieldCount).Select(x => record.GetName(x)).ToList();
        }

        /// <summary>
        /// Create a mapping between a type and a list of columns
        /// </summary>
        public static ValueMapping CreateMapping(Type type, IDataRecord record)
        {
            return CreateMapping(type, "", GetColumns(record), new Type[0]);
        }

        /// <summary>
        /// Returns true if the type is considered a navigation property (heuristically)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static bool IsEntityType(Type type)
        {
            //If the type have any properties marked with the key attribute, the type is an entity
            var anyKeys = type
                .GetProperties()
                .SelectMany(x => x.GetCustomAttributes(true).Select(y => y.GetType().Name))
                .Where(x => x == "KeyAttribute")
                .Any();

            return anyKeys;
        }

        /// <summary>
        /// True if this type can't be resolved by the mapper
        /// </summary>
        /// <param name="type">A type that isn't a simple type (<see cref="PathAccessor.IsSimpleType(Type)"/> returns false) </param>
        public static bool IsBlacklistedType(Type type)
        {
            if (type.IsInterface)
                return true;

            //Check if the type is a collection:
            if (type.IsGenericType)
            {
                var gen = type.GetGenericTypeDefinition();
                if (
                    gen == typeof(List<>) ||
                    gen == typeof(Dictionary<,>)
                    )
                    return true;
            }


            return false;
        }

        /// <summary>
        /// True if a prefix matches with a given column name
        /// </summary>
        static bool PrefixMatchesColumnName(string prefix, string columnName)
        {
            if (prefix == "") return true;

            prefix = prefix.ToLowerInvariant();
            columnName = columnName.ToLowerInvariant();

            if (prefix.Length > columnName.Length)
                return false;
            if (prefix.Length == columnName.Length)
                return prefix == columnName;

            return columnName[prefix.Length] == '_' && columnName.StartsWith(prefix);
        }

        /// <summary>
        /// Create a mapping between a type and a list of columns.
        /// </summary>
        /// <param name="prefix">Only look into columns with this prefix</param>
        public static ValueMapping CreateMapping(Type type, string prefix, IReadOnlyList<string> columns, IReadOnlyList<Type> chain)
        {
            if (chain.Contains(type))
            {
                //Recursive type:
                return new NullMapping(type);
            }
            //True if this mapping is for the top type
            var topLevel = !chain.Any();
            chain = chain.Concat(new[] { type }).ToList();

            if (PathAccessor.IsSimpleType(type))
            {

                var ixs =
                    columns
                    .Select((x, i) => (x, i))
                    .Where(x => PrefixMatchesColumnName(prefix, x.x))
                    .Select(X => (int?)X.i)
                    .ToList();
                if (ixs.Count > 1)
                {
                    var ixsStr = string.Join(", ", ixs);
                    throw new ArgumentException($"Multiple columns '{ixsStr}' matches name '{prefix}'");
                }

                var ix = ixs.SingleOrDefault();
                if (ix == null)
                    return new NullMapping(type);

                return new SingularMapping(type, ix.Value);
            }

            if (IsBlacklistedType(type))
            {
                return new NullMapping(type);
            }

            if (!topLevel && IsEntityType(type))
            {
                //Entity properties are not resolved
                return new NullMapping(type);
            }

            var constructors = type.GetConstructors();
            if (!constructors.Any())
            {
                return new NullMapping(type);
            }

            //Selects the first constructor with the most number of arguments that have a non-null mapping
            var (cons, consMap) = PickConstructor(constructors, prefix, columns, chain);
            if (consMap == null)
            {
                return new NullMapping(type);
            }

            var props = type.GetProperties().Where(x => x.GetSetMethod() != null);
            var propMap = MapProperties(props, prefix, columns, chain);

            //Remove property mapping of repeated constructor columns:
            var propMapFiltered = propMap
                .Where(x => !consMap.Any(cs => cs.Columns.SequenceEqual(x.Value.Columns)))
                .ToDictionary(x => x.Key, x => x.Value);

            return new CtorMapping(cons, consMap, propMapFiltered);
        }

        /// <summary>
        /// Gets the first constructor with the most number of arguments and a non-null mapping between all parameters and columns
        /// </summary>
        /// <param name="chain">The constructor chain parent of this pick constructor call. If a constructor its already on the chain, wont be picked in order to prevent recursive constructors</param>
        static (ConstructorInfo, IReadOnlyList<ValueMapping>) PickConstructor(IEnumerable<ConstructorInfo> constructors, string prefix, IReadOnlyList<string> columns, IReadOnlyList<Type> chain)
        {
            var cons = constructors
                .OrderByDescending(x => x.GetParameters().Length)
                .Select(x => new
                {
                    cons = x,
                    mapping = MapConstructor(x, prefix, columns, chain)
                })
                .Where(x => x.mapping != null)
                .FirstOrDefault();

            return (cons?.cons, cons?.mapping);
        }

        /// <summary>
        /// Map object property setters
        /// </summary>
        static IReadOnlyDictionary<PropertyInfo, ValueMapping> MapProperties(IEnumerable<PropertyInfo> props, string prefix, IReadOnlyList<string> columns, IReadOnlyList<Type> chain)
        {
            var maps = props
                .Select(x => new
                {
                    map = MapProperty(x.PropertyType, x.Name, prefix, columns, chain),
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
        static IReadOnlyList<ValueMapping> MapConstructor(ConstructorInfo cons, string prefix, IReadOnlyList<string> columns, IReadOnlyList<Type> chain)
        {
            var pars = cons.GetParameters();
            var type = cons.DeclaringType;

            var propMappings = pars
                .Select(x => MapProperty(x.ParameterType, x.Name, prefix, columns, chain))
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
        static ValueMapping MapProperty(Type type, string property, string prefix, IReadOnlyList<string> columns, IReadOnlyList<Type> chain)
        {
            return CreateMapping(type, prefix == "" ? property : ($"{prefix}_{property}"), columns, chain);
        }

    }
}
