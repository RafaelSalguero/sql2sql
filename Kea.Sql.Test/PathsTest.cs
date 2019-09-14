using Sql2Sql.Mapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Test
{
    /// <summary>
    /// Note que las pruebas de los Paths son importantes ya que 
    /// el GetPaths se usa por proyectos externos (forma parte de la API publica de kea.Sql)
    /// </summary>
    [TestClass]
    public class PathsTest
    {
        /// <summary>
        /// Verifica que las propiedades obtenidas por el PathAccesor.GetPaths sean las correctas.
        /// Debe de ignorar el ForeignKey
        /// </summary>
        [TestMethod]
        public void TestPaths()
        {
            var paths = PathAccessor
                .GetPaths(typeof(Test.ComplexTypes.Empresa))
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

        /// <summary>
        /// Revisa que se obtengan bien los paths de clases sin atributos de ComplexType ni Owned,
        /// debe de ignorar los listados
        /// </summary>
        [TestMethod]
        public void TestPathsSinAtributos()
        {
            var paths = PathAccessor
                .GetPaths(typeof(KeaSql.Test.ComplexTypes.Cliente2))
                .Paths
                .OrderBy(x => x.Key)
                ;

            var expected = new string[]
            {
                "Dir_Calle",
                "Dir_Colonia",
                "Dir_Numero",
                "Nombre",
            };
            var actual = paths.Select(x => x.Key).ToList();
            CollectionAssert.AreEqual(expected, actual);
        }

    }
}
