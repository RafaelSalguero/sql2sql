using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Sql2Sql.Mapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sql2Sql.Mapper.Test
{
    public class DicDataReader : IDataReader
    {
        public DicDataReader(IReadOnlyList<IReadOnlyList<KeyValuePair<string, object>>> data)
        {
            var f = data.First();
            this.FieldCount =f .Count;
            this. names = f.Select(x => x.Key).ToList();

            this.items = new Queue<IReadOnlyList<KeyValuePair<string, object>>>( data);
        }
        readonly IReadOnlyList<string> names;
        readonly Queue<IReadOnlyList<KeyValuePair<string, object>>> items;
        IReadOnlyList<KeyValuePair<string, object>> current;

        public bool Read()
        {
            if(items.Any())
            {
                current = items.Dequeue();
                return true;
            }
            current = null;
            return false;
        }

        public object this[int i] => current[i];

        public object this[string name] => current.Where(x => x.Key == name).Select(x => x.Value).First();

        public string GetName(int i) => names[i];

        public int FieldCount { get; }

        public int Depth => throw new NotImplementedException();

        public bool IsClosed => throw new NotImplementedException();

        public int RecordsAffected => throw new NotImplementedException();

        public T GetFieldValue<T>(int i )
        {
            var type = typeof(T);
            return (T)GetValue(i);
        }

        public bool GetBoolean(int i)
        {
            throw new NotImplementedException();
        }

        public byte GetByte(int i)
        {
            return (byte)this[i];
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
            return (DateTime)GetValue(i);
        }

        public decimal GetDecimal(int i)
        {
            return (decimal)GetValue(i);
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
            var ret = current[i].Value;
            if (ret == null)
                throw new Exception("Column is null");
            return ret;
        }

        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public bool IsDBNull(int i)
        {
            return current[i].Value == null || current[i].Value == DBNull.Value;
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public bool NextResult()
        {
            throw new NotImplementedException();
        }



        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

}
