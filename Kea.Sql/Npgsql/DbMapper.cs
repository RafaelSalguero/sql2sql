using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
        readonly ComplexTypePaths paths;
        readonly List<string> columns;
        readonly Dictionary<Type, TypeAccessor> accessors;
        readonly IDataRecord reader;


        public class AccessPath
        {
            public AccessPath(string name, Type propType, Type instanceType)
            {
                Name = name;
                PropType = propType;
                InstanceType = instanceType;
            }

            public string Name { get; }
            public Type PropType { get; }
            public Type InstanceType { get; }
        }
        public class ComplexTypePaths
        {
            public ComplexTypePaths(Dictionary<string, IReadOnlyList<AccessPath>> paths, IReadOnlyList<Type> types)
            {
                Paths = paths;
                Types = types;
            }

            public Dictionary<string, IReadOnlyList<AccessPath>> Paths { get; }
            public IReadOnlyList<Type> Types { get; }
        }

        /// <summary>
        /// Obtiene las rutas de los tipos complejos
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static ComplexTypePaths GetPaths(Type type)
        {
            //Obtener todas las propiedades que NO son complex type:
            var props = type.GetProperties();
            var simpleProps = props.Where(x => !SqlExpression.IsComplexType(x.PropertyType));
            var complexProps = props.Where(x => SqlExpression.IsComplexType(x.PropertyType));

            var paths = new Dictionary<string, IReadOnlyList<AccessPath>>();
            //Primero agregar las propiedades simples:
            foreach (var p in simpleProps)
            {
                paths.Add(p.Name, new[] { new AccessPath(p.Name, p.PropertyType, type) });
            }

            //Luego los tipos complejos:
            var types = new List<Type>();
            types.Add(type);
            foreach (var p in complexProps)
            {
                var subPaths = GetPaths(p.PropertyType);
                types.AddRange(subPaths.Types);
                var currPath = new AccessPath(p.Name, p.PropertyType, type);
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
        /// Lee el registro actual del DbDataReader llenando el objeto 'dest'
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="reader"></param>
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
                    //Establecer el valor siguiendo el path
                    object curr = dest;
                    var acc = accessors[path.First().InstanceType];
                    //Obtener el objeto al que se le va a asignar la propiedad:
                    foreach (var part in path.Take(path.Count - 1))
                    {
                        curr = acc[curr, part.Name];
                        if(curr == null)
                        {
                            throw new ArgumentException($"La propiedad de tipo complejo '{part.Name}' del tipo '{part.InstanceType}' no esta inicializada");
                        }
                        acc = accessors[part.PropType];
                    }
                    acc[curr, path.Last().Name] = value;
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"No se pudo asignar el valor '{value}' a la propiedad '{col}' del tipo '{typeof(T)}'", ex);
                }
            }
        }
    }
}
