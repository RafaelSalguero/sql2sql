using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sql2Sql.Mapper.Test
{
    /// <summary>
    /// Un corte diario establece en ceros todas las cajas de la sucursal donde es realizado. Esto debido a que representa el deposito al banco que se realiza, vaciando con este las cajas
    /// </summary>
    public class CorteDiario
    {
        public CorteDiario() { }
        public CorteDiario(DateTimeOffset fecha, int idSucursal, int idUsuario, int? idDepositoBanco)
        {
            Fecha = fecha;
            IdSucursal = idSucursal;
            IdUsuario = idUsuario;
        }

        public int IdRegistro { get; set; }

        /// <summary>
        /// Fecha en la que se realizó el corte
        /// </summary>
        public DateTimeOffset Fecha { get; set; }

        /// <summary>
        /// Sucursal donde se realiza el corte diario. Esto determina las cajas que se van a poner en 0, que son todas las cajas de todas las sucursales
        /// </summary>
        public int IdSucursal { get; set; }

        /// <summary>
        /// Usuario que realiza el corte diario
        /// </summary>
        public int IdUsuario { get; set; }


    }

    [TestClass]
    public class CorteTest
    {
        [TestMethod]
        public void Test()
        {
            var date = new DateTimeOffset(2000, 01, 26, 5, 6, 0, TimeSpan.Zero);
            var record = new[]
                {
                    new KeyValuePair<string, object>("IdRegistro", 1),
                    new KeyValuePair<string, object>("Fecha",date ),
                    new KeyValuePair<string, object>("IdSucursal", 1),
                    new KeyValuePair<string, object>("IdUsuario",  3),
                };

            var reader = new DicDataReader(new[] { record });
            var mapper = DbMapper.CreateReader<DicDataReader, CorteDiario>(reader);

            var items = mapper(reader);
            var curr = items.First();

            Assert.AreEqual(1, curr.IdRegistro);
            Assert.AreEqual(date, curr.Fecha);
            Assert.AreEqual(3, curr. IdUsuario);
        }
    }
}
