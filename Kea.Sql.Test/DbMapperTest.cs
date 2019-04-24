using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using KeaSql.Npgsql;
using KeaSql.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KeaSql.Test
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
        [TestMethod]
        public void ComplexTypeMapperTest()
        {
            var values = new[]
            {
                new KeyValuePair<string, object>("Dir_Personales_Telefono", "123"),
                new KeyValuePair<string, object>("Dir_Calle", "E Baca Calderon"),
                new KeyValuePair<string, object>("IdEstado", 2),
                new KeyValuePair<string, object>("Nombre", "Rafa"),
                new KeyValuePair<string, object>("Tipo", 1),
            };

            var record= new DicDataRecord(values);
            var mapper = new DbMapper<Cliente>(record);

            var dest = new Cliente();
            mapper.ReadCurrent(dest);

            Assert.AreEqual(dest.IdEstado, 2);
            Assert.AreEqual(dest.Nombre, "Rafa");
            Assert.AreEqual(dest.Dir.Personales.Telefono, "123");
            Assert.AreEqual(dest.Dir.Calle, "E Baca Calderon");
            Assert.AreEqual(dest.Tipo, TipoPersona.Moral);
        }
    }
}
