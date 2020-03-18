using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sql2Sql.Mapper.Test
{
    /// <summary>
    /// Un concepto de una póliza contable, note que ademas de guardarse en el sistema de contabilidad se guarda también un espejo de todas las pólizas en la base de datos
    /// </summary>
    public class ConceptoPolizaContable
    {
        public ConceptoPolizaContable() { }
        public ConceptoPolizaContable(string noCuenta, decimal importe, string descripcion)
        {
            NoCuenta = noCuenta;
            Importe = importe;
            Descripcion = descripcion;
        }

        public int IdRegistro { get; set; }

        /// <summary>
        /// Id de la póliza padre
        /// </summary>
        public int IdPolizaContable { get; set; }
        public PolizaContable PolizaContable { get; set; }

        /// <summary>
        /// Numero de cuenta a la que va dirigido
        /// </summary>
        public string NoCuenta { get; set; }

        /// <summary>
        /// Importe del concepto
        /// </summary>
        public decimal Importe { get; set; }
        /// <summary>
        /// Descripción del concepto
        /// </summary>
        public string Descripcion { get; set; }
    }

    /// <summary>
    /// Una poliza contable registrada en el sistema de contabilidad 
    /// </summary>
    public class PolizaContable
    {
        protected PolizaContable() { }
        public PolizaContable(
            string idSistemaContabilidad,
            int? idFactura,
            int? idRep,
            int idSucursal,
            string concepto,
            List<ConceptoPolizaContable> conceptos,
            ConceptoPolizaContable concepto2,
            DateTimeOffset fechaCreacion,
            DateTimeOffset fechaPoliza,
            int? idUsuarioCancelo,
            DateTimeOffset? fechaCancelacion

            )
        {
            IdSistemaContabilidad = idSistemaContabilidad;
            IdFactura = idFactura;
            Concepto2 = concepto2;
            IdREP = idRep;
            IdSucursal = idSucursal;
            Conceptos = conceptos;
            Concepto = concepto;
            FechaCreacion = fechaCreacion;
            FechaPoliza = fechaPoliza;
            IdUsuarioCancelo = idUsuarioCancelo;
            FechaCancelacion = fechaCancelacion;
        }

        public int IdRegistro { get; set; }

        /// <summary>
        /// Descripción de la póliza
        /// </summary>
        public string Concepto { get; set; }

        /// <summary>
        /// Identificador otorgado por el sistema de contabilidad. Este valor es null si la póliza fue registrada con una configuración de deshabiitar conexión con el sistema contable
        /// </summary>
        public string IdSistemaContabilidad { get; set; }

        /// <summary>
        /// Sucursal de la póliza
        /// </summary>
        public int IdSucursal { get; set; }

        /// <summary>
        /// Fecha de creación de la póliza
        /// </summary>
        public DateTimeOffset FechaCreacion { get; set; }

        /// <summary>
        /// Fecha con la que se registra la póliza en el sistema de contabilidad, no necesariamente es lo mismo que la fecha de creación
        /// </summary>
        public DateTimeOffset FechaPoliza { get; set; }

        /// <summary>
        /// Si esta es una póliza de cancelación de otra póliza, este es el id de la poliza origal
        /// </summary>
        public int? IdPolizaCancelada { get; set; }
        public PolizaContable PolizaCancelada { get; set; }

        /// <summary>
        /// Conceptos de la póliza
        /// </summary>
        public List<ConceptoPolizaContable> Conceptos { get; set; } = new List<ConceptoPolizaContable>();

        /// <summary>
        /// Concepto (PRUEBA)
        /// </summary>
        public ConceptoPolizaContable Concepto2 { get; set; }

        /// <summary>
        /// Concepto (PRUEBA)
        /// </summary>
        public ConceptoPolizaContable Concepto3 { get; set; }

        /// <summary>
        /// En caso de que esta póliza provenga de una factura, es el id de la factura. Esta unicidad es importante porque el query de FacturaView se basa en esta
        /// </summary>
        public int? IdFactura { get; set; }

        /// <summary>
        /// En caso de que esta póliza provenga de un REP, es el id del mismo
        /// </summary>
        public int? IdREP { get; set; }


        /// <summary>
        /// En caso de que esta póliza provenga de un depósito a banco, es ese depósito
        /// </summary>
        public int? IdDepositoBanco { get; set; }

        /// <summary>
        /// En caso de que esta póliza provenga de una entrega de ingresos a usuarios, es la entrega
        /// </summary>
        public int? IdEntregaIngresosAUsuario { get; set; }

        /// <summary>
        /// Indica el usuario que cancelo esta factura
        /// </summary>
        public int? IdUsuarioCancelo { get; set; }

        /// <summary>
        /// Fecha de la cancelación de la poliza
        /// </summary>
        public DateTimeOffset? FechaCancelacion { get; set; }

        /// <summary>
        /// En caso de que esta póliza provenga de una poliza cancelada este es el id de la factura que fue cancelada para originar esta poliza
        /// </summary>
        public int? IdFacturaCancelacion { get; set; }

        /// <summary>
        /// En caso de que esta póliza provenga de una poliza cancelada este es el id de la entregaIngresosAUsuario que fue cancelada para originar esta poliza
        /// </summary>
        public int? IdCancelacionEntregaIngresos { get; set; }
    }

    public class Cliente
    {

        /// <summary>
        /// Colonia de la dirección de los datos de facturación
        /// </summary>
        public int? IdDatosFacturacionColonia { get; set; }
        public Colonia DatosFacturacionColonia { get; set; }
    }

    public class UsuarioApi
    {
        public Cliente  Cliente { get; set; }
    }

    public class Municipio
    {
        public UsuarioApi UsuarioApi { get; set; }
    }

    /// <summary>
    /// Una colonia o asentamiento
    /// </summary>
    public class Colonia
    {
        public int IdMunicipio { get; set; }

        /// <summary>
        /// munincipio al que pertenece la colonia
        /// </summary>
        public Municipio Municipio { get; set; }
    }

    /// <summary>
    /// Una sucursal del sistema
    /// </summary>
    public class Sucursal
    {
        public Sucursal(
            int idDireccionColonia
            )
        {
            IdDireccionColonia = idDireccionColonia;
        }
        public Sucursal() { }

        public int IdDireccionColonia { get; set; }
        public Colonia DireccionColonia { get; set; }
    }

    /// <summary>
    /// Un corte diario establece en ceros todas las cajas de la sucursal donde es realizado. Esto debido a que representa el deposito al banco que se realiza, vaciando con este las cajas
    /// </summary>
    public class CorteDiario
    {
        public CorteDiario() { }
        public CorteDiario(DateTimeOffset fecha, int idSucursal, int idUsuario, PolizaContable polizaDepositoBanco)
        {
            Fecha = fecha;
            IdSucursal = idSucursal;
            IdUsuario = idUsuario;
            PolizaDepositoBanco = polizaDepositoBanco;
        }

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
        public Sucursal Sucursal { get; set; }

        /// <summary>
        /// Usuario que realiza el corte diario
        /// </summary>
        public int IdUsuario { get; set; }

        public int? IdPolizaDepositoBanco { get; set; }
        public PolizaContable PolizaDepositoBanco { get; set; }

        public PolizaContable PolizaDepositoBanco2 { get; set; }
    }

    [TestClass]
    public class RecursiveListTypeTest
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
            Assert.AreEqual(3, curr.IdUsuario);
        }
    }
}
