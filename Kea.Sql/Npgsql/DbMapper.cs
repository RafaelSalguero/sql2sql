using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using FastMember;
using KeaSql.SqlText;

namespace KeaSql.Npgsql
{
    /// <summary>
    /// Mapea un <see cref="IDataRecord"/> a un objeto
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DbMapper<T>
    {
        public DbMapper(IDataRecord reader)
        {
            this.reader = reader;

            var fCount = reader.FieldCount;
            this.columns = new List<string>();
            for (var i = 0; i < fCount; i++)
            {
                this.columns.Add(reader.GetName(i));
            }

            paths = GetPaths(typeof(T));
            accessors = paths.Types.ToDictionary(x => x, x => TypeAccessor.Create(x));
        }
        readonly ExprCast cast = new ExprCast();
        readonly ComplexTypePaths paths;
        readonly List<string> columns;
        readonly Dictionary<Type, TypeAccessor> accessors;
        readonly IDataRecord reader;


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

        /// <summary>
        /// Lista las columnas de una entidad, incluyendo recursivamente las propiedades de los tipos complejos
        /// </summary>
        public class ComplexTypePaths
        {
            public ComplexTypePaths(Dictionary<string, IReadOnlyList<AccessPathItem>> paths, IReadOnlyList<Type> types)
            {
                Paths = paths;
                Types = types;
            }

            /// <summary>
            /// Cada una de las columnas y su ruta de acceso.
            /// </summary>
            public Dictionary<string, IReadOnlyList<AccessPathItem>> Paths { get; }

            /// <summary>
            /// Todos los tipos de los que se extrajeron propiedades, el primer elemento es siempre el tipo de la entidad y los demás son los tipos complejos analizados
            /// </summary>
            public IReadOnlyList<Type> Types { get; }
        }

        /// <summary>
        /// Obtiene todas las rutas para acceder a todas las columnas de un tipo de entidad, incluyendo recursivamente las propiedades de los tipos complejos
        /// </summary>
        public static ComplexTypePaths GetPaths(Type type)
        {
            //Obtener todas las propiedades que NO son complex type:
            var props = type.GetProperties();
            var simpleProps = props.Where(x => !SqlExpression.IsComplexType(x.PropertyType));
            var complexProps = props.Where(x => SqlExpression.IsComplexType(x.PropertyType));

            var paths = new Dictionary<string, IReadOnlyList<AccessPathItem>>();
            //Primero agregar las propiedades simples:
            foreach (var p in simpleProps)
            {
                paths.Add(p.Name, new[] { new AccessPathItem(p.Name, p.PropertyType, type) });
            }

            //Luego los tipos complejos:
            var types = new List<Type>();
            types.Add(type);
            foreach (var p in complexProps)
            {
                var subPaths = GetPaths(p.PropertyType);
                types.AddRange(subPaths.Types);
                var currPath = new AccessPathItem(p.Name, p.PropertyType, type);
                foreach (var x in subPaths.Paths)
                {
                    paths.Add(p.Name + "_" + x.Key, new[] { currPath }.Concat(x.Value).ToList());
                }
            };

            return new ComplexTypePaths(paths, types);
        }

        static DateTimeOffset ToDateTimeOffset(DateTime date)
        {
            if (date.Kind == DateTimeKind.Unspecified)
                throw new ArgumentException("No se puede convertir un DateTime con Kind = 'Unspecified' a DateTimeOffset");

            DateTimeOffset ret = date;
            return ret;
        }

        static bool IsTypeOrNullable(Type testedType, Func<Type, bool> test, out Type nonNullType)
        {
            if (test(testedType))
            {
                nonNullType = testedType;
                return true;
            }
            if (testedType.IsGenericType && testedType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var t = testedType.GetGenericArguments()[0];
                nonNullType = t;
                return test(t);
            }
            nonNullType = null;
            return false;
        }

        object ReadColumn(IDataRecord reader, int column)
        {
            if (reader.IsDBNull(column))
            {
                return null;
            }
            var colType = paths.Paths[columns[column]].Last().PropType;
            var value = reader.GetValue(column);

            if (IsTypeOrNullable(colType, x => x == typeof(DateTimeOffset), out var _) && value is DateTime date)
            {
                return ToDateTimeOffset(date);
            }
            if (IsTypeOrNullable(colType, x => x.IsEnum, out var enumType))
            {
                //Si es enum:
                return Enum.ToObject(enumType, value);
            }
            return value;
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
            return acc[lastPath.Name] ;
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
        /// Lee el registro actual del DbDataReader llenando el objeto 'dest'
        /// </summary>
        public void ReadCurrent(T dest)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                if (!paths.Paths.TryGetValue(col, out var path))
                {
                    throw new ArgumentException($"No se encontró la columna '{col}' en el tipo {typeof(T)}");
                }

                object value;
                try
                {
                    value = ReadColumn(reader, i);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"No se pudo leer el valor de la columna '{col}'", ex);
                }

                try
                {
                    SetPathValue(dest, path, cast, value);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"No se pudo asignar el valor '{value}' a la propiedad '{col}' del tipo '{typeof(T)}'", ex);
                }
            }
        }
    }
}
