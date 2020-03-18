using Sql2Sql.Mapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sql2Sql.Mapper.Test;

namespace Sql2Sql.Test.ComplexTypes
{
    [TestClass]
   public class ComplexTest2
    {
        IReadOnlyList<KeyValuePair<string, object>> GetData1() => new[]
        {
                new KeyValuePair<string, object>("Id_Numero", 1),
                new KeyValuePair<string, object>("Id_Nombre", "Hola"),
        };

        [TestMethod]
        public void ReadTest()
        {
            var reader = new DicDataReader(new[] { GetData1() } );

            var read = DbMapper.CreateReader<DicDataReader, MigracionDb>(reader);
            var data =  read(reader).First();

            Assert.AreEqual(1, data.Id.Numero);
            Assert.AreEqual("Hola", data.Id.Nombre);
        }

        IReadOnlyList<KeyValuePair<string, object>> GetData2() => new[]
        {
                new KeyValuePair<string, object>("Nombre", "Rafa"),
                new KeyValuePair<string, object>("Dir_Numero", 123),
                new KeyValuePair<string, object>("Dir_Calle", "Revolucion"),
                new KeyValuePair<string, object>("Dir_Colonia", "Campestre"),
        };

        /// <summary>
        /// Prueba que jale con un objeto que tiene propiedades de navegación recursivas. 
        /// Tambien prueba que funcione mezclar un tipo con constructor vacío (Cliente2) con uno con constructor 
        /// parametrizado (Direccion)
        /// </summary>
        [TestMethod]
        public void ReadRecTest()
        {
            var reader = new DicDataReader(new[] { GetData2() });

            var read = DbMapper.CreateReader<DicDataReader, Cliente2>(reader);
            var data = read(reader).First();

            Assert.AreEqual("Rafa", data.Nombre);
            Assert.AreEqual(123, data.Dir.Numero);
            Assert.AreEqual("Revolucion", data.Dir.Calle);
            Assert.AreEqual("Campestre", data.Dir.Colonia);
        }
    }
}
