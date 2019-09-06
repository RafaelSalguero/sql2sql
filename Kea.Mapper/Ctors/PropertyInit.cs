using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Kea.MapperCtor;

namespace Kea.Ctors
{
    /// <summary>
    /// Instrucciones para inicializar un objeto
    /// </summary>
    class ObjectInit
    {
        public ObjectInit(ConstructorInfo constructor, IReadOnlyList<ValueInit> parameters, IReadOnlyList<PropertyInit> propertyInits)
        {
            Constructor = constructor;
            Parameters = parameters;
            PropertyInits = propertyInits;
        }

        /// <summary>
        /// El constructor que se va a usar
        /// </summary>
        public ConstructorInfo Constructor { get; }

        /// <summary>
        /// Inicializadores de los parametros del constructor
        /// </summary>
        public IReadOnlyList<ValueInit> Parameters { get; }

        /// <summary>
        /// Inicializadores de las propiedades del objeto
        /// </summary>
        public IReadOnlyList<PropertyInit> PropertyInits { get; }
    }

    /// <summary>
    /// Una inicialización de cierta propiedad 
    /// </summary>
    public class PropertyInit
    {
        public PropertyInit(object value)
        {
            Value = value;
        }

        /// <summary>
        /// Inicializador del valor
        /// </summary>
        public object Value { get; }
    }


    /// <summary>
    /// Una inicialización de cierto valor ya sea de una propiedad o de un parámetro
    /// </summary>
    public class ValueInit
    {
        public ValueInit(Type type, IReadOnlyList<KeyValuePair<string, object>> colValues)
        {
            Type = type;
            ColValues = colValues;
        }

        /// <summary>
        /// Tipo a inicializar
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Valores del DataRecord que afectan a la inicialización de las propiedades
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, object>> ColValues { get; }
    }
}
