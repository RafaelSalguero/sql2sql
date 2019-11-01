using Sql2Sql.Ctors;
using Sql2Sql.Mapper.ComplexTypes;
using Sql2Sql.Mapper.ILCtors;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Mapper
{
    /// <summary>
    /// Lee datos ya de un <see cref="IDataReader"/>
    /// </summary>
    public class DbReader
    {
        /// <summary>
        /// Lee todos los elementos de un <see cref="IDataReader"/>
        /// </summary>
        public static List<T> Read<T, TReader>(TReader reader, ColumnMatchMode mode = ColumnMatchMode.Ignore)
            where TReader : IDataReader
        {
            var ret = new List<T>();
            var mapper = new DbMapper<T>(reader);
            while (reader.Read())
            {
                var item = mapper.ReadCurrent();
                ret.Add(item);
            }
            return ret;
        }

        /// <summary>
        /// Lee todos los elementos de un <see cref="DbDataReader"/>
        /// </summary>
        public static async Task<List<T>> ReadAsync<T, TReader>(TReader reader, ColumnMatchMode mode = ColumnMatchMode.Ignore)
            where TReader : DbDataReader
        {
            var ret = new List<T>();
            var mapper = new DbMapper<T>(reader);
#if NET40
            //El DbDataReader de .NET 4.0 no tiene ReadAsync
            while (reader.Read())
#else
            while (await reader.ReadAsync())
#endif
            {
                var item = mapper.ReadCurrent();
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
      /// <summary>
      /// Crea un DbMapper a partir de un IDataRecord
      /// </summary>
      /// <param name="reader"></param>
        public DbMapper(IDataRecord reader)
        {
            this.reader = reader;
            mapping = Mapper.Ctors.MappingLogic.CreateMapping(typeof(T), reader);
        }
        readonly ValueMapping mapping;
        readonly IDataRecord reader;


        /// <summary>
        /// Lee el valor actual. 
        /// 
        /// Para crear una instancia del tipo se hace lo siguiente:
        /// - Primero se busca un constructor sin argumentos, en caso de que se encuentre, la inicialización del objeto es asignando sus propiedades
        /// - Si no, se busca un constructor único con argumentos, si hay más de uno lanza una excepción
        /// </summary>
        public T ReadCurrent()
        {
            var type = typeof(T);
            return (T)MapperCtor.ReadCurrent(reader, mapping);
        }
    }
}
