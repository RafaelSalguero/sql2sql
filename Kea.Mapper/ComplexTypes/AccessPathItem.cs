using System;
using System.Collections.Generic;
using System.Text;

namespace Kea.Mapper.ComplexTypes
{
    /// <summary>
    /// Un elemento en una ruta para acceder a cierta propiedad de una entidad.
    /// Para obtener el mapeo de columnas a rutas de propiedades use el <see cref="PathAccessor.GetPaths(Type)"/>
    /// </summary>
    public class AccessPathItem
    {
        /// <summary>
        /// Crea un access path item
        /// </summary>
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
