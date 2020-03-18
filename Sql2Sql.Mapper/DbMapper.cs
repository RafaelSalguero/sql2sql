using Sql2Sql.Ctors;
using Sql2Sql.Mapper.ComplexTypes;
using Sql2Sql.Mapper.ILCtors;
using System;
using System.Collections.Concurrent;
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
        public static List<T> Read<T, TReader>(TReader reader)
            where TReader : IDataReader
        {
            var readerFunc = DbMapper.CreateReader<TReader, T>(reader);
            return readerFunc(reader);
        }

        /// <summary>
        /// Lee todos los elementos de un <see cref="DbDataReader"/>
        /// </summary>
        public static async Task<List<T>> ReadAsync<T, TReader>(TReader reader)
            where TReader : DbDataReader
        {
            //TODO: Poner el Async
            var readerFunc = DbMapper.CreateReader<TReader, T>(reader);
            return readerFunc(reader);
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


    public class DbMapper
    {
        static ConcurrentDictionary<(Type item, Type reader), Delegate> readerCache = new ConcurrentDictionary<(Type item, Type reader), Delegate>();

        /// <summary>
        /// Create a data reader function. If the type pair is repeated, the same function is returned
        /// </summary>
        public static Func<TReader, List<TItem>> CreateReader<TReader, TItem>(TReader reader)
            where TReader : IDataReader
        {
            var ret = readerCache.GetOrAdd((typeof(TItem), typeof(TReader)), key => CreateReaderSlow<TReader, TItem>(reader));
            return (Func<TReader, List<TItem>>)ret;
        }


        /// <summary>
        /// Create a non-cached data reader function
        /// </summary>
        static Func<TReader, List<TItem>> CreateReaderSlow<TReader, TItem>(TReader reader)
            where TReader : IDataReader
        {
            var mapping = Mapper.Ctors.MappingLogic.CreateMapping(typeof(TItem), reader);
            var expr = ILCtorLogic.GenerateReaderMethod<TReader, TItem>(mapping);
            var func = expr.Compile();
            return func;
        }
    }
}
