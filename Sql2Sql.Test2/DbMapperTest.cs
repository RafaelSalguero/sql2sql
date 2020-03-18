using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Sql2Sql.Mapper;
using Sql2Sql.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sql2Sql.Mapper.Test;

namespace Sql2Sql.Test
{
    public class DicDataRecord : IDataRecord
    {
        public DicDataRecord(IReadOnlyList<KeyValuePair<string, object>> data)
        {
            this.data = data;
        }
        readonly IReadOnlyList<KeyValuePair<string, object>> data;

        public object this[int i] => data[i];

        public object this[string name] => data.Where(x => x.Key == name).Select(x => x.Value).First();

        public int FieldCount => data.Count;

        public bool GetBoolean(int i)
        {
            throw new NotImplementedException();
        }

        public byte GetByte(int i)
        {
            throw new NotImplementedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            throw new NotImplementedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public string GetDataTypeName(int i)
        {
            throw new NotImplementedException();
        }

        public DateTime GetDateTime(int i)
        {
            throw new NotImplementedException();
        }

        public decimal GetDecimal(int i)
        {
            throw new NotImplementedException();
        }

        public double GetDouble(int i)
        {
            throw new NotImplementedException();
        }

        public Type GetFieldType(int i)
        {
            throw new NotImplementedException();
        }

        public float GetFloat(int i)
        {
            throw new NotImplementedException();
        }

        public Guid GetGuid(int i)
        {
            throw new NotImplementedException();
        }

        public short GetInt16(int i)
        {
            throw new NotImplementedException();
        }

        public int GetInt32(int i)
        {
            return (int)GetValue(i);
        }

        public long GetInt64(int i)
        {
            throw new NotImplementedException();
        }

        public string GetName(int i)
        {
            return data[i].Key;
        }

        public int GetOrdinal(string name)
        {
            throw new NotImplementedException();
        }

        public string GetString(int i)
        {
            return (string)GetValue(i);
        }

        public object GetValue(int i)
        {
            return data[i].Value;
        }

        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public bool IsDBNull(int i)
        {
            return GetValue(i) == null;
        }
    }

    [TestClass]
    public class DbMapperTest
    {
        IReadOnlyList<KeyValuePair<string, object>> GetComplexTypeTestData() => new[]
            {
                new KeyValuePair<string, object>("Dir_Personales_Telefono", "123"),
                new KeyValuePair<string, object>("Dir_Calle", "E Baca Calderon"),
                new KeyValuePair<string, object>("IdEstado", 2),
                new KeyValuePair<string, object>("Nombre", "Rafa"),
                new KeyValuePair<string, object>("Apellido", "Salguero"),
                new KeyValuePair<string, object>("Tipo", 1),
                new KeyValuePair<string, object>("Precio", 10.5M),
                new KeyValuePair<string, object>("IdRegistro", 3),
                new KeyValuePair<string, object>("Fecha", new DateTime(1994,01,26)),
            };



        [TestMethod]
        public void ComplexTypeReadOnlyMapperTest()
        {
            var values = GetComplexTypeTestData();

            var record = new DicDataReader(new[] { values } );
            var mapper = DbMapper.CreateReader<DicDataReader, ClienteRO>(record);

            var dest = mapper(record).First();

            Assert.AreEqual(dest.IdRegistro, 3);
            Assert.AreEqual(dest.IdEstado, 2);
            Assert.AreEqual(dest.Nombre, "Rafa");
            Assert.AreEqual(dest.Dir.Personales.Telefono, "123");
            Assert.AreEqual(dest.Dir.Calle, "E Baca Calderon");
            Assert.AreEqual(dest.Tipo, TipoPersona.Moral);

            Assert.AreEqual(dest.Precio, 10.5M);
            Assert.AreEqual(dest.Fecha, new DateTime(1994, 01, 26));
        }

        [TestMethod]
        public void ComplexTypeMapperTest()
        {
            var values = GetComplexTypeTestData();

            var record = new DicDataReader(new[] { values } );
            var mapper = DbMapper.CreateReader<DicDataReader, Cliente>(record);

            var dest = mapper(record).First();

            Assert.AreEqual(dest.IdRegistro, 3);
            Assert.AreEqual(dest.IdEstado, 2);
            Assert.AreEqual(dest.Nombre, "Rafa");
            Assert.AreEqual(dest.Dir.Personales.Telefono, "123");
            Assert.AreEqual(dest.Dir.Calle, "E Baca Calderon");
            Assert.AreEqual(dest.Tipo, TipoPersona.Moral);

            Assert.AreEqual(dest.Precio, 10.5M);
        }

        [TestMethod]


        public void SingularTypeMapperTest()
        {
            var values = new[]
            {
                new KeyValuePair<string, object>("Nombre", "Rafa"),
            };

            var record = new DicDataReader(new[] { values } );
            var mapper =  DbMapper.CreateReader<DicDataReader, string>(record);

            var dest = mapper(record).First();

            Assert.AreEqual(dest, "Rafa");
        }
    }
}
