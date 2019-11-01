using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
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
                    new KeyValuePair<string, object>("Type", 1),
                    new KeyValuePair<string, object>("Dir1_Street", "Frida Kahlo"),
                    new KeyValuePair<string, object>("Dir1_Personal_Phone", "555 555"),
                    new KeyValuePair<string, object>("Dir1_Personal_Cellphone", "01 23 45"),
                    new KeyValuePair<string, object>("Date", new DateTime(2000,02,03)),
                };

            var reader = new DicDataReader(new[] { record });
            var mapper = new DbMapper<Customer2>(reader);

            reader.Read();
            var curr = mapper.ReadCurrent();

            Assert.AreEqual(1, curr.Id);
            Assert.AreEqual("Rafa", curr.Name);
            Assert.AreEqual(CustomerType.Other, curr.Type);
            Assert.AreEqual("Frida Kahlo", curr.Dir1.Street);
            Assert.AreEqual("555 555", curr.Dir1.Personal.Phone);
            Assert.AreEqual("01 23 45", curr.Dir1.Personal.Cellphone);
            Assert.AreEqual(new DateTime(2000, 02, 03), curr.Date);
        }
    }
}
