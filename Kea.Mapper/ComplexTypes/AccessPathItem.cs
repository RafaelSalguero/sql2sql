using System;
using System.Collections.Generic;
using System.Text;

namespace Kea.Mapper
{
    /// <summary>
    /// Un elemento en una ruta para acceder a cierta propiedad
    /// </summary>
    public class AccessPathItem
    {
        public AccessPathItem(string name, Type propType, Type instanceType)
        {
            Name = name;
            PropType = propType;
            InstanceType = instanceType;
        }

        /// <summary>
        /// Nombre de la propiedad
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Tipo de la propiedad
        /// </summary>
        public Type PropType { get; }

        /// <summary>
        /// Tipo al que pertenece la propiedad
        /// </summary>
        public Type InstanceType { get; }
    }
}
