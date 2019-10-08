using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Mapper.ILCtors
{
    /// <summary>
    /// A mapping between a data reader and a constructor / property settings
    /// </summary>
    class CtorMapping
    {
        public CtorMapping(ConstructorInfo constructor, IReadOnlyList<int> constructorColumnMapping, IReadOnlyDictionary<PropertyInfo, int> propertyMapping)
        {
            Constructor = constructor;
            ConstructorColumnMapping = constructorColumnMapping;
            PropertyMapping = propertyMapping;
        }

        /// <summary>
        /// The constructor used for initializing the instance
        /// </summary>
        public ConstructorInfo Constructor { get; }

        /// <summary>
        /// Each element represents a mapping between a constructor parameter and a column index.
        /// The index of the array is the index of the constructor parameter 
        /// The value of the element is the index of the column
        /// </summary>
        public IReadOnlyList<int> ConstructorColumnMapping { get; }

        /// <summary>
        /// Mapping between properties and column indices
        /// </summary>
        public IReadOnlyDictionary<PropertyInfo, int> PropertyMapping { get; }
    }
}
