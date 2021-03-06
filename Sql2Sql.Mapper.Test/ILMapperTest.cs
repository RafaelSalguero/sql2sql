using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sql2Sql.Mapper.ILCtors;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Sql2Sql.Mapper.Test
{
    [TestClass]
    public class ILMapperTest
    {
        [TestMethod]
        public void SimpleILTest()
        {
            var records = new[]
            {
                new []
                {
                    new KeyValuePair<string, object>("Id", 1),
                    new KeyValuePair<string, object>("Name", "Rafa"),
                    new KeyValuePair<string, object>("Age", 25),
                },
                 new []
                {
                    new KeyValuePair<string, object>("Id", 2),
                    new KeyValuePair<string, object>("Name", "Ale"),
                    new KeyValuePair<string, object>("Age", null),
                },
            };
            var reader = new DicDataReader(records);
            var t = typeof(Customer1);
            var rt = typeof(DicDataReader);
            var mapping = new CtorMapping(
                t.GetConstructor(new Type[0]),
                new ValueMapping[0],
                new Dictionary<PropertyInfo, ValueMapping>
                {
                    { t .GetProperty("Id"),  new SingularMapping(typeof(int),  0 )},
                    { t .GetProperty("Name") , new SingularMapping(typeof(string),  1 ) },
                    { t .GetProperty("Age"), new SingularMapping(typeof(int?),  2 ) }
                }
                );

            var method = Sql2Sql.Mapper.ILCtors.ILCtorLogic.GenerateReaderMethod<DicDataReader, Customer1>(mapping);
            var comp = method.Compile();

            var call = comp(reader);
        }

    }
}
