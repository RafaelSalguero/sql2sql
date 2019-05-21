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
                    PathAccessor.SetPathValue(dest, path, cast, value);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"No se pudo asignar el valor '{value}' a la propiedad '{col}' del tipo '{typeof(T)}'", ex);
                }
            }
        }
    }
}
