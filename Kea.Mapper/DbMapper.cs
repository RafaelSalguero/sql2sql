using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kea.Mapper
{
    /// <summary>
    /// Lee datos ya de un <see cref="IDataReader"/>
    /// </summary>
    public class DbReader
    {
        /// <summary>
        /// Lee todos los elementos de un <see cref="IDataReader"/>
        /// </summary>
        public static List<T> Read<T>(IDataReader reader, ColumnMatchMode mode = ColumnMatchMode.Ignore )
        {
            var ret = new List<T>();
            var mapper = new DbMapper<T>(reader);
            while (reader.Read())
            {
                var item = mapper.ReadCurrent(mode);
                ret.Add(item);
            }
            return ret;
        }

        /// <summary>
        /// Lee todos los elementos de un <see cref="DbDataReader"/>
        /// </summary>
        public static async Task<List<T>> ReadAsync<T>(DbDataReader reader, ColumnMatchMode mode = ColumnMatchMode.Ignore)
        {
            var ret = new List<T>();
            var mapper = new DbMapper<T>(reader);
            while (await reader.ReadAsync())
            {
                var item = mapper.ReadCurrent(mode);
                ret.Add(item);
            }
            return ret;
        }
    }

    /// <summary>
    /// Indica que tipo de validación se debe de hacer en el mapeo de  columnas
    /// </summary>
    public enum ColumnMatchMode
    {
        /// <summary>
        /// Todas las columnas en la fila deben de existir en la clase, no importa que existan columnas en la clase que no estén en la fila
        /// </summary>
        Source,

        /// <summary>
        /// Sólo se mapean las columnas que existen tanto en la clase como en la fila, no importa que existan columnas en la fila que no estén en la clase o vicevera
        /// </summary>
        Ignore,
    }

    /// <summary>
    /// Mapea un <see cref="IDataRecord"/> a un objeto
    /// </summary>
    /// <typeparam name="T">Tipo del registro de salida</typeparam>
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

            paths = PathAccessor.GetPaths(typeof(T));
        }
        readonly ExprCast cast = new ExprCast();
        readonly ComplexTypePaths paths;
        readonly List<string> columns;
        readonly IDataRecord reader;


        static DateTimeOffset ToDateTimeOffset(DateTime date)
        {
            if (date.Kind == DateTimeKind.Unspecified)
                throw new ArgumentException("No se puede convertir un DateTime con Kind = 'Unspecified' a DateTimeOffset");

            DateTimeOffset ret = date;
            return ret;
        }

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

        /// <summary>
        /// Lee el valor de una columna de <paramref name="reader"/>, considerando los tipos de la clase ligada a este <see cref="DbMapper{T}"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        object ReadClassColumn(IDataRecord reader, int column)
        {
            var colType = paths.Paths[columns[column]].Last().PropType;
            return ReadColumn(reader, column, colType);
        }

        /// <summary>
        /// Lee el valor de una columna de <see cref="reader"/>
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



        /// <summary>
        /// Lee el registro actual del DbDataReader llenando el objeto 'dest'
        /// </summary>
        void ReadCurrentClass(T dest, ColumnMatchMode mode)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                if (!paths.Paths.TryGetValue(col, out var path))
                {
                    switch (mode)
                    {
                        case ColumnMatchMode.Source:
                            throw new ArgumentException($"No se encontró la columna '{col}' en el tipo {typeof(T)}");
                        case ColumnMatchMode.Ignore:
                            continue;
                        default:
                            throw new ArgumentException(nameof(mode));
                    }
                }

                object value;
                try
                {
                    value = ReadClassColumn(reader, i);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"No se pudo leer el valor de la columna '{col}'", ex);
                }

                try
                {
                    PathAccessor.SetPathValue(dest, path, cast, value);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"No se pudo asignar el valor '{value}' de tipo '{value.GetType()}' a la propiedad '{col}' del tipo '{typeof(T)}'", ex);
                }
            }
        }

        /// <summary>
        /// Lee el registro actual, en caso de que sea un valor singular, por ejemplo, un sólo numero o cadena
        /// </summary>
        /// <returns></returns>
        T ReadCurrentSingular()
        {
            if (columns.Count != 1)
                throw new ArgumentException("El query devolvió más de 1 columna, y el tipo de retorno del query es uno singular");

            var ret = ReadColumn(reader, 0, typeof(T));
            return (T)cast.Cast(typeof(T), ret);
        }

        /// <summary>
        /// Lee el valor actual
        /// </summary>
        public T ReadCurrent(ColumnMatchMode mode)
        {
            var type = typeof(T);
            if (!PathAccessor.IsSimpleType(type))
            {
                if (!type.IsClass)
                    throw new ArgumentException($"El tipo '{type}' debe de ser una clase");
                var cons = type.GetConstructor(new Type[0]);
                if (cons == null)
                    throw new ArgumentException($"El tipo '{type}' no tiene un constructor sin argumentos por lo que no se puede utilizar como retorno de un query");

                var ret = (T)cons.Invoke(new object[0]);
                ReadCurrentClass(ret, mode);
                return ret;
            }

            return ReadCurrentSingular();
        }
    }
}
