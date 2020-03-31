using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sql2Sql.Mapper.Test
{
    [TestClass]
    public class Customer2Test
    {
        [TestMethod]
        public void Customer2()
        {
            var record = new[]
                {
                    new KeyValuePair<string, object>("Id", 1),
                    new KeyValuePair<string, object>("Name", "Rafa"),
                    new KeyValuePair<string, object>("Type", null),
                    new KeyValuePair<string, object>("Type2", 1),
                    new KeyValuePair<string, object>("Dir1_Street", "Frida Kahlo"),
                    new KeyValuePair<string, object>("Dir1_Personal_Phone", "555 555"),
                    new KeyValuePair<string, object>("Dir1_Personal_Cellphone", "01 23 45"),
                    new KeyValuePair<string, object>("Date", new DateTime(2000,02,03)),
                    new KeyValuePair<string, object>("NullStr", DBNull.Value),
                };

            var reader = new DicDataReader(new[] { record });
            var mapper = DbMapper.CreateReader<DicDataReader, Customer2>(reader);

            var items = mapper(reader);
            var curr = items.First();

            Assert.AreEqual(1, curr.Id);
            Assert.AreEqual("Rafa", curr.Name);
            Assert.AreEqual("Frida Kahlo", curr.Dir1.Street);
            Assert.AreEqual("555 555", curr.Dir1.Personal.Phone);
            Assert.AreEqual("01 23 45", curr.Dir1.Personal.Cellphone);
            Assert.AreEqual(new DateTime(2000, 02, 03), curr.Date);
            Assert.AreEqual(null, curr.Type);
            Assert.AreEqual(CustomerType.Other, curr.Type2);
            Assert.AreEqual(null, curr.NullStr);
        }

        public class ObjTest
        {
            public ObjTest(object value, object description, object isNull, object dbNull)
            {
                Value = value;
                Description = description;
                IsNull = isNull;
                DbNull = dbNull;
            }

            public object Value { get; }
            public object Description { get; }
            public object IsNull { get; }
            public object DbNull { get; }
        }

        /// <summary>
        /// Mapping of object typed properties
        /// </summary>
        [TestMethod]
        public void ObjectTest()
        {
            var record = new[]
             {
                    new KeyValuePair<string, object>("Value", 1),
                    new KeyValuePair<string, object>("Description", "Rafa"),
                    new KeyValuePair<string, object>("IsNull", null),
                    new KeyValuePair<string, object>("DbNull", DBNull.Value),
                };

            var reader = new DicDataReader(new[] { record });
            var mapper = DbMapper.CreateReader<DicDataReader, ObjTest>(reader);

            var items = mapper(reader);
            var ret = items.First();

            Assert.AreEqual(1, ret.Value);
            Assert.AreEqual("Rafa", ret.Description);
            Assert.AreEqual(null, ret.IsNull);
            Assert.AreEqual(null, ret.DbNull);
        }
        /// <summary>
        /// Mapping of object typed properties
        /// </summary>
        [TestMethod]
        public void SingleObjectTest()
        {
            var record = new[]
             {
                    new KeyValuePair<string, object>("Value", 1),
                };

            var reader = new DicDataReader(new[] { record });
            var mapper = DbMapper.CreateReader<DicDataReader, object>(reader);

            var items = mapper(reader);
            var ret = items.First();

            Assert.AreEqual(1, ret);
        }
    }
}
