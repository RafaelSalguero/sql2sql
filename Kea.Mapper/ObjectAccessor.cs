using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Kea.Mapper
{
    /// <summary>
    /// Lee propiedades de objetos dinámicamente. Es un reemplazo de la libreria FastMember para poder soportar .NET Standard 1.0
    /// </summary>
    class ObjectAccessor
    {
        ObjectAccessor(object instance)
        {
            this.instance = instance;
            this.properties = instance.GetType().GetProperties();
        }

        /// <summary>
        /// Crea un nuevo ObjectAccesor para cierta instancia
        /// </summary>
        public static  ObjectAccessor Create(object instance) => new ObjectAccessor(instance);

        readonly object instance;
        readonly Type type;
        readonly IReadOnlyList<PropertyInfo> properties;

        public object this[string property]
        {
            get => properties.First(x => x.Name == property).GetValue(instance);
            set => properties.First(x => x.Name == property).SetValue(instance, value);
        }


    }
}
