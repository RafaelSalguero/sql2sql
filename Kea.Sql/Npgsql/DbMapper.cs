using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using FastMember;
using KeaSql.ComplexTypes;
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

            paths = PathAccessor.GetPaths(typeof(T));
            accessors = paths.Types.ToDictionary(x => x, x => TypeAccessor.Create(x));
        }
        readonly ExprCast cast = new ExprCast();
        readonly ComplexTypePaths paths;
        readonly List<string> columns;
        readonly Dictionary<Type, TypeAccessor> accessors;
        readonly IDataRecord reader;



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

        object ReadClassColumn(IDataRecord reader, int column)
        {
            var colType = paths.Paths[columns[column]].Last().PropType;
            return ReadColumn(reader, column, colType);
        }

        object ReadColumn(IDataRecord reader, int column, Type colType)
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
        void ReadCurrentClass<T>(T dest)
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
                    throw new ArgumentException($"No se pudo asignar el valor '{value}' a la propiedad '{col}' del tipo '{typeof(T)}'", ex);
                }
            }
        }

        /// <summary>
        /// Lee el registro actual
        /// </summary>
        /// <returns></returns>
        T ReadCurrentSingular<T>()
        {
            if (columns.Count != 1)
                throw new ArgumentException("El query devolvió más de 1 columna, y el tipo de retorno del query es uno singular");

            return (T)ReadColumn(reader, 0, typeof(T));
        }

        /// <summary>
        /// Lee el valor actual
        /// </summary>
        public T ReadCurrent<T>()
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
                ReadCurrentClass(ret);
                return ret;
            }

            return ReadCurrentSingular<T>();
        }
    }
}
