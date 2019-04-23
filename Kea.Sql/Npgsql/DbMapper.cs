using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using FastMember;

namespace KeaSql.Npgsql
{
    internal class DbMapper<T>
    {
        public DbMapper(DbDataReader reader)
        {
            this.accessor = TypeAccessor.Create(typeof(T));
            this.reader = reader;

            var fCount = reader.FieldCount;
            this.columns = new List<string>();
            for (var i = 0; i < fCount; i++)
            {
                this.columns.Add(reader.GetName(i));
            }

            this.types = typeof(T).GetProperties().ToDictionary(x => x.Name, x => x.PropertyType);
        }
        readonly Dictionary<string, Type> types;
        readonly List<string> columns;
        readonly DbDataReader reader;
        readonly TypeAccessor accessor;

        static DateTimeOffset ToDateTimeOffset(DateTime date)
        {
            if (date.Kind == DateTimeKind.Unspecified)
                throw new ArgumentException("No se puede convertir un DateTime con Kind = 'Unspecified' a DateTimeOffset");

            DateTimeOffset ret = date;
            return ret;
        }

        static bool IsTypeOrNullable(Type testedType, Type destType)
        {
            if (testedType == destType)
                return true;
            if (testedType.IsGenericType && testedType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return testedType.GetGenericArguments()[0] == destType;
            }
            return false;
        }

        object ReadColumn(DbDataReader reader, int column)
        {
            if (reader.IsDBNull(column))
            {
                return null;
            }
            var colType = types[columns[column]];
            var value = reader.GetValue(column);

            if (IsTypeOrNullable(colType, typeof(DateTimeOffset)) && value is DateTime date)
            {
                return ToDateTimeOffset(date);
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
                if (!types.ContainsKey(col))
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
                    accessor[dest, col] = value;
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"No se pudo asignar el valor '{value}' a la propiedad '{col}' del tipo '{typeof(T)}'", ex);
                }
            }
        }
    }
}
