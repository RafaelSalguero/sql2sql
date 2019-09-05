using Kea.Mapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Test.ComplexTypes
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
            var record = new DicDataRecord(GetData1());

            var mapper = new DbMapper<MigracionDb>(record);
            var data = mapper.ReadCurrent(ColumnMatchMode.Source);

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
            var record = new DicDataRecord(GetData2());
            var mapper = new DbMapper<Cliente2>(record);
            var data = mapper.ReadCurrent(ColumnMatchMode.Source);

            Assert.AreEqual("Rafa", data.Nombre);
            Assert.AreEqual(123, data.Dir.Numero);
            Assert.AreEqual("Revolucion", data.Dir.Calle);
            Assert.AreEqual("Campestre", data.Dir.Colonia);
        }
    }
}
