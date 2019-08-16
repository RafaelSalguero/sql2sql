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
    public class ComplexTypeTest
    {
        /// <summary>
        /// Verifica que las propiedades obtenidas por el PathAccesor.GetPaths sean las correctas
        /// </summary>
        [TestMethod]
        public void TestPaths()
        {
            var paths = PathAccessor
                .GetPaths(typeof(Empresa))
                .Paths
                .OrderBy(x => x.Key)
                ;

            var expected = new string[]
            {
                "Datos_Rfc",
                "Datos_Tipo",
                "Destacado",
                "FechaMod",
                "IdCuenta",
                "IdRegistro",
                "Nombre"
            };
            var actual = paths.Select(x => x.Key).ToList();
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
