﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Mapper.ILCtors
{
    /// <summary>
    /// A mapping for a data reader, can be a <see cref="SingularMapping"/> or a <see cref="CtorMapping"/>
    /// </summary>
    abstract class ValueMapping
    {
        protected ValueMapping(Type type)
        {
            Type = type;
        }

        /// <summary>
        /// The type of the value
        /// </summary>
        public Type Type { get; }
    }

    /// <summary>
    /// A mapping for a single data reader column
    /// </summary>
    class SingularMapping : ValueMapping
    {
        public SingularMapping(Type type, int columnId) : base(type)
        {
            ColumnId = columnId;
        }

        /// <summary>
        /// The column ID
        /// </summary>
        public int ColumnId { get; }
    }

    /// <summary>
    /// A mapping between a data reader and a constructor / property settings
    /// </summary>
    class CtorMapping : ValueMapping
    {
        public CtorMapping(ConstructorInfo constructor, IReadOnlyList<ValueMapping> constructorColumnMapping, IReadOnlyDictionary<PropertyInfo, ValueMapping> propertyMapping) : base(constructor.DeclaringType)
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
        /// Each element represents a mapping for a constructor parameter
        /// The index of the array is the index of the constructor parameter 
        /// The value of the element is the desired mapping
        /// </summary>
        public IReadOnlyList<ValueMapping> ConstructorColumnMapping { get; }

        /// <summary>
        /// Property setter mapping
        /// </summary>
        public IReadOnlyDictionary<PropertyInfo, ValueMapping> PropertyMapping { get; }
    }
}
