using Sql2Sql.Mapper.ComplexTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

[assembly:InternalsVisibleTo("Sql2Sql.Mapper.Test")]
namespace Sql2Sql.Mapper
{
    /// <summary>
    /// Funciones para acceder a las propiedades de una entidad, tomando en cuenta los tipos complejos
    /// </summary>
    public static class PathAccessor
    {
        /// <summary>
        /// Determina si un tipo tiene el ComplexTypeAttribute o el OwnedAttribute
        /// </summary>
        public static bool IsComplexType(Type t)
        {
            var attNames = t.GetCustomAttributesData().Select(x => x.Constructor.DeclaringType).Select(x => x.Name);
            return attNames.Contains("ComplexTypeAttribute") || attNames.Contains("OwnedAttribute");
        }

        /// <summary>
        /// Obtiene la instancia del penultimo elemento del path, de tal manera que ya sea leer o escribir el path se realizará sobre esta instancia
        /// </summary>
        static object GetLastPathInstance(object dest, IReadOnlyList<AccessPathItem> path)
        {
            //Establecer el valor siguiendo el path
            object curr = dest;
            //Obtener el objeto al que se le va a asignar la propiedad:
            foreach (var part in path.Take(path.Count - 1))
            {
                var acc = ObjectAccessor.Create(curr);
                var nextCurr = acc[part.Name];
                if (nextCurr == null)
                {
                    var cons = part.PropType.GetConstructor(new Type[0]);
                    if (cons == null)
                    {
                        throw new ArgumentException($"La propiedad de tipo complejo '{part.Name}' del tipo '{part.InstanceType}' no esta inicializada y no tiene constructor por default");
                    }
                    nextCurr = cons.Invoke(new object[0]);
                    acc[part.Name] = nextCurr;
                }

                curr = nextCurr;
            }
            return curr;
        }

        /// <summary>
        /// Obtiene el valor de una columna de una entidad
        /// </summary>
        /// <param name="source">Entidad</param>
        /// <param name="path">Ruta de la propiedad</param>
        public static object GetPathValue(object source, List<AccessPathItem> path)
        {
            var curr = GetLastPathInstance(source, path);
            var acc = ObjectAccessor.Create(curr);

            var lastPath = path.Last();
            return acc[lastPath.Name];
        }

        /// <summary>
        /// Establece el valor de una columna de una entidad 
        /// </summary>
        /// <param name="dest">Entidad a la cual se le va a asignar el valor</param>
        /// <param name="path">Ruta de la propiedad a asignar</param>
        /// <param name="cast">Realiza las conversiones de tipos</param>
        /// <param name="value">Valor a asignar a la propiedad</param>
        public static void SetPathValue(object dest, List<AccessPathItem> path, ExprCast cast, object value)
        {
            var curr = GetLastPathInstance(dest, path);
            var acc = ObjectAccessor.Create(curr);

            var lastPath = path.Last();
            acc[lastPath.Name] = cast.Cast(lastPath.PropType, value);
        }

        /// <summary>
        /// Returns true if the type is a value type, a primitive, the type String, a byte array or object
        /// </summary>
        /// <param name="type">The type to check</param>
        public static bool IsSimpleType(this Type type)
        {
            return
                type.IsPrimitive ||
                type.IsEnum ||
                type == typeof(string) ||
                type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) ||
                type == typeof(TimeSpan) ||
                type == typeof(byte[]) ||
                type == typeof(decimal) ||
                type == typeof(object) || 
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSimpleType(type.GetGenericArguments()[0]))
                ;
        }

        /// <summary>
        /// Obtiene todas las rutas de un tipo de entidad que corresponden a columnas de la base de datos.
        /// Para esto se analizan los tipos y los atributos de Entity Framework
        /// </summary>
        /// <param name="type">Tipo de entidad</param>
        public static ComplexTypePaths GetPaths(Type type)
        {
            return GetPathsRecGuard(type, new Stack<Type>());
        }

        /// <summary>
        /// Devuelve true si un tipo NO debe de ser considerado como complex type porque se considera que es un navigation collection
        /// </summary>
        static bool IsNavigationCollection(PropertyInfo prop)
        {
            //Si tiene el atributo NavigatonPropertyAttribute es una coleccion de navegación
            var atts = prop.GetCustomAttributes(true)
                .Select(x => x.GetType().Name);
            if (atts.Any(x => x == "NavigationPropertyAttribute"))
                return true;

            var type = prop.PropertyType;
            if (type.IsGenericType)
            {
                //Si es un listado también:
                var gen = type.GetGenericTypeDefinition();
                return
                    gen == typeof(List<>)
                    || gen == typeof(IReadOnlyList<>)
                    || gen == typeof(ICollection<>)
                    || gen == typeof(HashSet<>);
            }
            return false;
        }

        /// <summary>
        /// Devuelve true si la propiedad es un navigation property, por lo que NO es un complex type
        /// </summary>
        static bool IsNavigationProperty(PropertyInfo prop)
        {
            //Nombre del atributo, note que buscamos a los atributos por nombre y
            //no por tipo, esto para encajar tanto los de EF6 como los de EFCore
            var fkName = "ForeignKeyAttribute";

            var type = prop.PropertyType;
            //Si es simple type devuelve false:
            if (IsSimpleType(type))
                return false;

            //Si es un tipo no simple y tiene el ForeignKey attribute es un navigation property:
            var atts = prop.GetCustomAttributes(true)
               .Select(x => x.GetType().Name);
            if (atts.Any(x => x == fkName))
                return true;

            //Todos los atributos de todas las propiedades del tipo:
            var propAtts = prop
                .DeclaringType
                .GetProperties()
                .SelectMany(x => x.GetCustomAttributesData());

            CustomAttributeData e;
            //Si existe algun atributo ForeignKey que apunte a esta propiedad, la propiedad
            //es un navigation property

            var otroFk = propAtts
                .Where(x => x. Constructor.DeclaringType.Name == fkName)
                .Where(x => x.ConstructorArguments.Count == 1)
                .Where(x => prop.Name.Equals(x.ConstructorArguments[0].Value))
                .Any();

            return otroFk;
        }

        /// <summary>
        /// Obtiene todas las rutas para acceder a todas las columnas de un tipo de entidad, incluyendo recursivamente las propiedades de los tipos complejos
        /// </summary>
        /// <param name="recTypes">Tipos que ya fueron navegados por el GetPaths, esto sirve para detectar e impedir que existan tipos recursivos</param>
        static ComplexTypePaths GetPathsRecGuard(Type type, Stack<Type> recTypes)
        {
            if (recTypes.Contains(type))
            {
                //El tipo es recursivo, ya que ya fue procesado por una llamada mas arriba en el stack, asi que devuelve un resultado vacío:
                return new ComplexTypePaths(new Dictionary<string, List<AccessPathItem>>(), new List<Type>());
            }

            //Obtener todas las propiedades que NO son complex type:
            var props = type.GetProperties();
            var simpleProps = props.Where(x => !IsComplexType(x.PropertyType) && IsSimpleType(x.PropertyType));
            var complexProps = props.Where(x => !IsSimpleType(x.PropertyType) && !IsNavigationCollection(x) && !IsNavigationProperty(x));

            var paths = new Dictionary<string, List<AccessPathItem>>();

            //Primero agregar las propiedades simples:
            foreach (var p in simpleProps)
            {
                paths.Add(p.Name, new[] { new AccessPathItem(p.Name, p.PropertyType, type) }.ToList());
            }

            //Luego los tipos complejos:
            var types = new List<Type>();
            types.Add(type);

            //Indicar a los hijos que si aparece mas abajo en la jerarquía el tipo actual se debe de ignorar:
            recTypes.Push(type);
            foreach (var p in complexProps)
            {
                var subPaths = GetPathsRecGuard(p.PropertyType, recTypes);
                types.AddRange(subPaths.Types);
                var currPath = new AccessPathItem(p.Name, p.PropertyType, type);
                foreach (var x in subPaths.Paths)
                {
                    paths.Add(p.Name + "_" + x.Key, new[] { currPath }.Concat(x.Value).ToList());
                }
            };
            //Quitar al tipo actual de la jerarquía
            recTypes.Pop();

            return new ComplexTypePaths(paths, types);
        }
    }
}
