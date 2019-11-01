using Sql2Sql.Ctors;
using Sql2Sql.Mapper;
using Sql2Sql.Mapper.ComplexTypes;
using Sql2Sql.Mapper.ILCtors;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Ctors
{
    /// <summary>
    /// Se encarga de la construcción de los tipos mapeados
    /// </summary>
    static class MapperCtor
    {
        /// <summary>
        /// Las columnas del data reader y los paths del objeto
        /// </summary>
        public class ColPaths
        {
            public ColPaths(IReadOnlyDictionary<string, int> columns, ComplexTypePaths paths)
            {
                Columns = columns;
                Paths = paths;
            }

            /// <summary>
            /// Diccionario con los nombres de las columnas del data reader y sus indices
            /// </summary>
            public IReadOnlyDictionary<string, int> Columns { get; }

            /// <summary>
            /// Las rutas del objeto
            /// </summary>
            public ComplexTypePaths Paths { get; }
        }

        /// <summary>
        /// Indica la forma en la que se creará la instancia de un tipo que va a leer el mapper
        /// </summary>
        public enum InitMode
        {
            /// <summary>
            /// El tipo es simple, no se llamará a ningún constructor
            /// </summary>
            SimpleType,
            /// <summary>
            /// Se usará el constructor por default
            /// </summary>
            PublicDefaultConstructor,
            /// <summary>
            /// Se usará el único constructor público con argumentos
            /// </summary>
            SingleParametizedConstructor,
        }

        /// <summary>
        /// Lee el registro actual, en caso de que sea un valor singular, por ejemplo, un sólo numero o cadena
        /// </summary>
        /// <returns></returns>
        static object ReadCurrentSingular(IDataRecord reader, SingularMapping mapping)
        {
            var ret = ReadColumn(reader, mapping.ColumnId, mapping.Type);
            return cast.Cast(mapping.Type, ret);
        }

        /// <summary>
        /// Lee el valor actual. 
        /// Para crear una instancia del tipo se hace lo siguiente:
        /// - Se inicializa buscando el constructor con la mayor cantidad de argumentos
        /// - Las propiedades que no fueron asignadas por constructor se inicalizan estableciendo las propiedades
        /// </summary>
        public static object ReadCurrent(IDataRecord reader, ValueMapping mapping)
        {
            return Init(reader, mapping);
        }

        static object Init(IDataRecord reader, ValueMapping mapping)
        {
            switch (mapping)
            {
                case Mapper.ILCtors.SingularMapping sing:
                    return ReadCurrentSingular(reader, sing);
                case Mapper.ILCtors.CtorMapping ctor:
                    return InitObject(reader, ctor);
                case Mapper.ILCtors.NullMapping n:
                    throw new ArgumentException($"Could not found any mapping for the type '{n.Type}'");
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Inicializa un objeto dado su constructor, escribe las propiedades de las columnas que no estuvieron incluídas en los parámetros del constructor
        /// </summary>
        static object InitObject(IDataRecord reader, CtorMapping mapping)
        {
            var pars = mapping.ConstructorColumnMapping.Select(x => Init(reader, x)).ToArray();
            var instance = mapping.Constructor.Invoke(pars);

            var props = mapping.PropertyMapping.Select(x => new
            {
                prop = x.Key,
                value = Init(reader, x.Value)
            }).ToList();

            foreach (var p in props)
            {
                p.prop.SetValue(instance, p.value);
            }

            return instance;
        }

        static ExprCast cast = new ExprCast();

        /// <summary>
        /// Determina si cierta prueba la pasa el tipo, o el tipo interno de un Nullable
        /// </summary>
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

        static DateTimeOffset ToDateTimeOffset(DateTime date)
        {
            if (date.Kind == DateTimeKind.Unspecified)
                throw new ArgumentException("No se puede convertir un DateTime con Kind = 'Unspecified' a DateTimeOffset");

            DateTimeOffset ret = date;
            return ret;
        }

        /// <summary>
        /// Lee el valor de una columna de un IDataRecord
        /// </summary>
        /// <param name="reader">Fila a leer</param>
        /// <param name="column">Índice de la columna</param>
        /// <param name="colType">Tipo de la columna</param>
        /// <returns>El valor de la columna</returns>
        static object ReadColumn(IDataRecord reader, int column, Type colType)
        {
            if (reader.IsDBNull(column))
            {
                return null;
            }

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
    }
}
