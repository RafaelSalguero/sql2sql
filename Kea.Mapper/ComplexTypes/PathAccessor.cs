using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kea.Mapper
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
            var attNames = t.CustomAttributes.Select(x => x.AttributeType).Select(x => x.Name);
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
        public static object GetPathValue(object source, IReadOnlyList<AccessPathItem> path)
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
        public static void SetPathValue(object dest, IReadOnlyList<AccessPathItem> path, ExprCast cast, object value)
        {
            var curr = GetLastPathInstance(dest, path);
            var acc = ObjectAccessor.Create(curr);

            var lastPath = path.Last();
            acc[lastPath.Name] = cast.Cast(lastPath.PropType, value);
        }

        /// <summary>
        /// Returns true if the type is a value type, a primitive, the type String or a byte array. 
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
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSimpleType(type.GetGenericArguments()[0]))
                ;
        }

        public static ComplexTypePaths GetPaths(Type type)
        {
            return GetPathsRecGuard(type, new Stack<Type>());
        }

        /// <summary>
        /// Devuelve true si un tipo NO debe de ser considerado como complex type porque se considera que es un navigation property
        /// </summary>
        static bool IsNavigationCollection(Type type)
        {
            if (type.IsGenericType)
            {
                var gen = type.GetGenericTypeDefinition();
                return
                    gen == typeof(List<>)
                    || gen == typeof(IReadOnlyList<>)
                    || gen == typeof(HashSet<>);
            }
            return false;
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
                return new ComplexTypePaths(new Dictionary<string, IReadOnlyList<AccessPathItem>>(), new List<Type>());
            }

            //Obtener todas las propiedades que NO son complex type:
            var props = type.GetProperties();
            var simpleProps = props.Where(x => !IsComplexType(x.PropertyType) && IsSimpleType(x.PropertyType));
            var complexProps = props.Where(x => !IsSimpleType(x.PropertyType) && !IsNavigationCollection(x.PropertyType));

            var paths = new Dictionary<string, IReadOnlyList<AccessPathItem>>();

            //Primero agregar las propiedades simples:
            foreach (var p in simpleProps)
            {
                paths.Add(p.Name, new[] { new AccessPathItem(p.Name, p.PropertyType, type) });
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
