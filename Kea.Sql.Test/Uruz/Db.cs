using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Test.Uruz
{
    /// <summary>
    /// Tipo complejo que representa una dirección. No incluye el estado ni la ciudad. Note que la colonia se debe de almacenar por fuera de este tipo, y que de la 
    /// colonia parte el codigo postal, el municipio, ciudad y estado
    /// </summary>
    [ComplexType]
    public class Direccion
    {
        public Direccion() { }
        public Direccion(string calle, string noExterior, string noInterior)
        {
            Calle = calle;
            NoExterior = noExterior;
            NoInterior = noInterior;
        }

        public string Calle { get; set; }
        public string NoExterior { get; set; }
        public string NoInterior { get; set; }
    }
    public enum TipoCaptura
    {
        Manual,
        DesdeGuia
    }

    public enum TipoFactura
    {
        Contado = 0,
        [Description("Crédito")]
        Credito = 1
    }

    public enum OrigenFactura
    {
        /// <summary>
        /// La factura fue creada manualmente por el usuario
        /// </summary>
        Manual = 0,
        /// <summary>
        /// Factura de guias de contado
        /// </summary>
        GuiasContado = 1,
        /// <summary>
        /// Factura con guias de un contrarecibo de cobranza
        /// </summary>
        Cobranza = 2,

        /// <summary>
        /// Factura de las guias de público general
        /// </summary>
        PublicoGeneral = 3,

        /// <summary>
        /// Factura desde el modulo de facturación online
        /// </summary>
        FacturacionOnline = 4,

        /// <summary>
        /// Factura creada de las guías de publico general de forma manual
        /// </summary>
        PublicoGeneralManual = 5,

        /// <summary>
        /// Factura creada de las guías de publico general de forma automática
        /// </summary>
        PublicoGeneralAutomatico = 6,

        /// <summary>
        /// Factura creada en una nota de crédito
        /// </summary>
        NotaDeCredito = 7,

        /// <summary>
        /// Factura creada automaticamente cuando se paga una guía de contado
        /// </summary>
        AutomaticaGuiaPagadaContado = 8,

        /// <summary>
        /// Factura creada para el pago de otra
        /// </summary>
        REP = 9,

        /// <summary>
        /// Factura creada a partir de un conjunto de guías de prepago manualmente
        /// </summary>
        GuiasPPManual = 10,

        /// <summary>
        /// Factura creada automáticamente al comprar el paquete de guías de PP
        /// </summary>
        GuiaPPAutomatica = 11,
        NotaDeCargo = 12,
    }

    /// <summary>
    /// Una factura electrónica / CFDI. Note que esta clase no tiene información relacionada con la lógica de negocios del sistema, solamente tiene información propia de los CFDIs
    /// </summary>
    public class Factura
    {
        public Factura() { }
       

        [Key]
        public int IdRegistro { get; set; }

        /// <summary>
        /// Fecha en la que se creó la factura
        /// </summary>
        public DateTimeOffset FechaCreacion { get; set; }

        /// <summary>
        /// Fecha de vencimiento de la factura, en caso de que sea de crédito
        /// </summary>
        public DateTimeOffset? FechaVencimiento { get; set; }

        /// <summary>
        /// Fecha que se va a utilizar para timbrar la factura
        /// </summary>
        public DateTimeOffset FechaFactura { get; set; }

        /// <summary>
        /// Id de Cliente de la factura, solo puede ser null en caso de que la factura sea de público general
        /// </summary>
        public int? IdCliente { get; set; }

        /// <summary>
        /// Sucursal de cobranza de la factura, normalmente es la sucursal del cliente
        /// </summary>
        public int? IdSucursalCobranza { get; set; }

        public ReceptorFactura ReceptorFactura { get; set; } = new ReceptorFactura();
        public EmisorFactura EmisorFactura { get; set; } = new EmisorFactura();

        /// <summary>
        /// Numero de cuenta ordenante
        /// </summary>
        public string NoCuentaOrdenante { get; set; }

        /// <summary>
        /// Numero de cuenta beneficiaria
        /// </summary>
        public string NoCuentaBeneficiaria { get; set; }

        /// <summary>
        /// Si esta es una factura de corrección de otra factura, este es el id de la facturaorigal.
        /// Tiene indice porque no pueden haber multiples facturas que sean correción de la misma factura. 
        /// Esta unicidad es importante porque el query de FacturaView se basa en esta
        /// </summary>
        [Index(IsUnique = true)]
        [ForeignKey(nameof(Factura.FacturaCorreccionOrigen))]
        public int? IdFacturaCorreccionOrigen { get; set; }
        public Factura FacturaCorreccionOrigen { get; set; }


        /// <summary>
        /// Sucursal de la factura, según las pláticas con el contador todas las facturas obligatoriamente deben de tener una sucursal asignada
        /// </summary>
        public int IdSucursal { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string ClaveSatRegimenFiscalEmisor { get; set; }

        /// <summary>
        /// Id del metodo de pago del SAT
        /// </summary>
        public int IdMetodoPagoSAT { get; set; }

        /// <summary>
        /// Id de la forma de pago del SAT
        /// </summary>
        public int IdFormaPagoSAT { get; set; }

        public string ClaveSatUsoReceptor { get; set; }

        /// <summary>
        /// Id de Moneda con la que se opera la factura
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string ClaveSatMoneda { get; set; }

        [Range((double)0.000001, (double)decimal.MaxValue, ErrorMessage = "El tipo de cambio debe ser mayor a cero")]
        public decimal TipoCambio { get; set; }


        ///<summary>
        ///Serie de la factura
        /// </summary>
        [Index("IX_SerieFolio", Order = 0, IsUnique = true)]
        public string Serie { get; set; }

        /// <summary>
        /// Folio de la factura, consecutivo y unico
        /// </summary>
        [Index("IX_SerieFolio", Order = 1, IsUnique = true)]
        public int Folio { get; set; }

        /// <summary>
        /// Origen de la factura
        /// </summary>
        public OrigenFactura Origen { get; set; }

        /// <summary>
        /// Fecha con la que se debe de registrar la póliza contable de esta factura
        /// </summary>
        public DateTimeOffset FechaPolizaContable { get; set; }

        /// <summary>
        /// Observaciones de la factura
        /// </summary>
        public string Observaciones { get; set; }

      
        /// <summary>
        /// Indica la forma en que la factura fue capturada
        /// </summary>
        public TipoCaptura TipoCaptura { get; set; }

        /// <summary>
        /// Id de la colonia del cliente
        /// </summary>
        public int? IdColonia { get; set; }

        /// <summary>
        /// Dirección del cliente
        /// </summary>
        public Direccion Direccion { get; set; }

        /// <summary>
        /// Correo al que se enviará la factura
        /// </summary>
        public string CorreoFacturacion { get; set; }

      
        /// <summary>
        /// Indica que esta factura fue pagada sin REPs, ya sea de crédito o de contado
        /// esto implica que a pesar de que no tenga REPs el saldo de la factura va a ser 0, y se
        /// va a considerar un abono a la factura igual al total de la misma en el view y en los abonos a crédito de los clientes.
        /// 
        /// Este valor lo tienen las facturas de crédito/contado que fueron pagadas con el antigüo proceso de cobranza pero que no tienen REP.
        /// 
        /// Fue necesario introducir este campo para igualar los saldos de los créditos en la migración del proceso de cobranza anterior al nuevo proceso de cobranza con REPs
        /// </summary>
        public bool PagadaSinRep { get; set; }

        /// <summary>
        /// Indica que esta factura esta registrada como que este de PUE, pero que en realidad es de crédito. Pudiendo ser que este o no cobrada por la cobranza anteriior
        /// Estas facturas provienen del antiguo proceso de cobranza, y deben de ser contabilizadas como un cargo para el crédito del cliente, siempre y cuando no esten canceladas.
        /// 
        /// Todas estas facturas deberán de ser canceladas y refacturadas con el tipo PPD para que se le puedan agregar los REPs correspondientes
        /// </summary>
        public bool PuePeroCredito { get; set; }

        /// <summary>
        /// Indica que esta factura esta registrada como PPD, pero que en realidad es de contado, por lo que no debe de afectar a los cargos de crédito
        /// 
        /// Estas facturas provienen de las facturas de contado que anteriormente fueron marcadas incorrectamente como PPD.
        /// </summary>
        public bool PpdPeroContado { get; set; }

        /// <summary>
        /// Es el código postal que indica el lugar de expedición de la factura, este se obtiene de la sucursal al momento de registrarla
        /// </summary>
        public string CodigoPostal { get; set; }
    }

    /// <summary>
    /// Un concepto de una factura
    /// </summary>
    public class ConceptoFactura
    {
        public ConceptoFactura() { }
      

        [Key]
        public int IdRegistro { get; set; }

        /// <summary>
        /// Factura padre de estos conceptos
        /// </summary>
        public int IdFactura { get; set; }

        /// <summary>
        /// Articulo del concepto
        /// </summary>
        public int IdArticulo { get; set; }

        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Descripcion { get; set; }

        /// <summary>
        /// La tasa de IVA que aplica para el concepto (del 0 al 1)
        /// </summary>
        public decimal TasaIva { get; set; }

        /// <summary>
        /// La tasa de Retención de IVA que aplica para el concepto (del 0 al 1)
        /// </summary>
        public decimal TasaRetencionIva { get; set; }

        /// <summary>
        /// La tasa de IEPS que aplica para el concepto (del 0 al 1)
        /// </summary>
        public decimal? TasaIeps { get; set; }

        /// <summary>
        /// Clave del SAT para el producto o servicio indicado
        /// </summary>
        public int IdProductoSAT { get; set; }

        /// <summary>
        /// Clave SAT de la unidad del presente concepto
        /// </summary>
        public int IdUnidadSAT { get; set; }

        /// <summary>
        /// En caso de que la factura padre de este concepto sea una nota de crédito esta columna es la Factura a la cual hace referencia este concepto en la nota de crédito
        /// </summary>
        public int? IdFacturaReferencia { get; set; }

        /// <summary>
        /// Indica si este concepto de el de ajuste de tipo de persona de física a moral
        /// </summary>
        public bool EsDeAjusteTipoPersona { get; set; }
    }


    /// <summary>
    /// Datos del emisor de una factura
    /// </summary>
    [ComplexType]
    public class EmisorFactura
    {
        public EmisorFactura() { }
        public EmisorFactura(string rfc, string nombre)
        {
            Rfc = rfc;
            Nombre = nombre;
        }

        /// <summary>
        /// Atributo requerido para registrar la Clave del Registro Federal de Contribuyentes correspondiente al contribuyente emisor del comprobante.
        /// </summary>
        public string Rfc { get; set; }

        /// <summary>
        /// Atributo opcional para registrar el nombre, denominación o razón social del contribuyente emisor del comprobante.
        /// </summary>
        public string Nombre { get; set; }
    }

    public enum TipoPersona
    {
        Fisica,
        Moral
    }

    /// <summary>
    /// Datos del receptor de una factura
    /// </summary>
    [ComplexType]
    public class ReceptorFactura
    {
        public ReceptorFactura() { }
        public ReceptorFactura(
            string rfc,
            string nombre,
            string residenciaFiscal,
            string numeroRegistroIdentidadFiscal,
            TipoPersona tipoPersona
        )
        {
            Rfc = rfc;
            Nombre = nombre;
            ResidenciaFiscal = residenciaFiscal;
            NumeroRegistroIdentidadFiscal = numeroRegistroIdentidadFiscal;
            TipoPersona = tipoPersona;
        }

        /// <summary>   
        /// Atributo requerido para precisar la Clave del Registro Federal de Contribuyentes correspondiente al contribuyente receptor del comprobante.
        /// </summary>
        public string Rfc { get; set; }

        /// <summary>
        /// Atributo opcional para precisar el nombre, denominación o razón social del contribuyente receptor del comprobante.
        /// </summary>
        public string Nombre { get; set; }

        /// <summary>
        /// Atributo condicional para registrar la clave del país de residencia para efectos fiscales del receptor del comprobante, cuando se trate de un extranjero, y que es conforme con la especificación ISO 3166-1 alpha-3. Es requerido cuando se incluya el complemento de comercio exterior o se registre el atributo NumRegIdTrib.
        /// </summary>
        public string ResidenciaFiscal { get; set; }

        /// <summary>
        /// Atributo condicional para expresar el número de registro de identidad fiscal del receptor cuando sea residente en el extranjero. Es requerido cuando se incluya el complemento de comercio exterior.
        /// </summary>
        public string NumeroRegistroIdentidadFiscal { get; set; }

        /// <summary>
        /// Tipo de persona que es el receptor de esta factura
        /// </summary>
        public TipoPersona TipoPersona { get; set; }


    }

    /// <summary>
    /// VIEW de las facturas
    /// </summary>
    public class FacturaView
    {
        /// <summary>
        /// Id de la factura
        /// </summary>
        [Key]
        [ForeignKey(nameof(Factura))]
        public int IdFactura { get; set; }
        public Factura Factura { get; set; }

        /// <summary>
        /// Fecha de creación de la factura
        /// </summary>
        public DateTimeOffset FechaCreacion { get; set; }

        /// <summary>
        /// Si la factura es PPD siempre y cuando no tenga la marca de PpdPeroContado,
        /// o si la factura es PUE pero tiene la marca PuePeroCredito
        /// </summary>
        public bool EsCredito { get; set; }

        /// <summary>
        /// Si el método de pago es PPD
        /// </summary>
        public bool EsPPD { get; set; }

        /// <summary>
        /// Si esta cancelada, la fecha de cancelación
        /// </summary>
        public DateTimeOffset? FechaCancelacion { get; set; }

        /// <summary>
        /// Si esta cancelada, el motivo de cancelación
        /// </summary>
        public string MotivoCancelacion { get; set; }

        public decimal Total { get; set; }
        /// <summary>
        /// Si es de crédito, es el saldo restante por pagar, considerando las notas de crédito y los REPs.
        /// Si es de contado, es 0
        /// </summary>
        public decimal Saldo { get; set; }

        /// <summary>
        /// Si el saldo es 0. Las de crédito inician en false y se pagan cuando los REPs/notas de crédito dejan el saldo en 0.
        /// Para las de contado siempre es true
        /// </summary>
        public bool Pagada { get; set; }

        /// <summary>
        /// Si la factura es de crédito es true si ya esta pagada o false si no esta pagada.
        /// Si la factura es de contado es null
        /// </summary>
        public bool? PagadaCredito { get; set; }

        /// <summary>
        /// Si es de crédito y esta pagada por completo, es la fecha del último pago.
        /// Si no es null
        /// </summary>
        public DateTimeOffset? FechaPago { get; set; }

        public decimal Subtotal { get; set; }
        /// <summary>
        /// Importe total de IVA de la factura
        /// </summary>
        public decimal Iva { get; set; }

        /// <summary>
        /// Importe total de retención de IVA
        /// </summary>
        public decimal RetencionIva { get; set; }

        /// <summary>
        /// Importe total de IEPS
        /// </summary>
        public decimal Ieps { get; set; }

        /// <summary>
        /// Importe descontado a la factura debido a las notas de crédito que se le han aplicado
        /// </summary>
        public decimal ImporteNotasCredito { get; set; }

        /// <summary>
        /// Cadena donde estan todos los folios de las notas de crédito que se le han aplicado a la factura, separadas por coma (,)
        /// </summary>
        public string FoliosNotasCredito { get; set; }

        /// <summary>
        /// Importe descontado a la factura debido a todos los REPs que le han aplicado
        /// </summary>
        public decimal ImporteReps { get; set; }

        /// <summary>
        /// Fecha mas reciente de todos los REPs, es la fecha del último pago
        /// </summary>
        public DateTimeOffset? FechaUltRep { get; set; }

        /// <summary>
        /// Ultimo numero de parcialidad de los abonos de REPs aplicados a esta factura
        /// </summary>
        public int? UltNumParcialidad { get; set; }

        /// <summary>
        /// Folios de los REPs que estan abonando a esta factura, separados por coma
        /// </summary>
        public string FolioReps { get; set; }

        /// <summary>
        /// Id de la factura que corrigió a esta factura o null si esta factura no tiene ninguna corrección
        /// </summary>
        [ForeignKey(nameof(FacturaCorrigio))]
        public int? IdFacturaCorrigio { get; set; }
        public Factura FacturaCorrigio { get; set; }

        /// <summary>
        /// Si esta factura es una nota de crédito, es el Id de la factura original a la que esta nota de crédito esta afectando
        /// </summary>
        [ForeignKey(nameof(NotaCreditoFacturaOriginal))]
        public int? IdNotaCreditoFacturaOriginal { get; set; }
        public Factura NotaCreditoFacturaOriginal { get; set; }

        /// <summary>
        /// UUID de la factura si es que esta timbrada
        /// </summary>
        public string Uuid { get; set; }

        /// <summary>
        /// Fecha de timbrado si es que esta timbrada
        /// </summary>
        public DateTimeOffset? FechaTimbrado { get; set; }

        /// <summary>
        /// Si la factura esta contabilizada, es el Id de la póliza contable
        /// </summary>
        public int? IdPolizaContable { get; set; }

        /// <summary>
        /// Si la factura esta cancelada y esa cancelación esta contabilizada, es el Id de la póliza contable que contabiliza a la cancelación
        /// </summary>
        public int? IdPolizaCancelacion { get; set; }

        /// <summary>
        /// Cargos al crédito de la factura, se ve afectado por las banderas de PuePeroCredito y PpdPeroContado
        /// </summary>
        public decimal CargoCredito { get; set; }

        /// <summary>
        /// Abonos al crédito, se ve afectado por la bandera de PagadaSinReps
        /// </summary>
        public decimal AbonoCredito { get; set; }

        /// <summary>
        /// Si la factura esta cancelada
        /// </summary>
        public bool Cancelada { get; set; }

        /// <summary>
        /// Id del cliente de la factura
        /// </summary>
        public int? IdCliente { get; set; }

        /// <summary>
        /// El total de las guias incluídas en esta factura, note que no necesariamente este va a encajar con el total de las facturas
        /// </summary>
        public decimal TotalGuias { get; set; }

        /// <summary>
        /// El Iva de las guías incluidas en esta factura, note que no necesariamente este va a encajar con el iva de las facturas
        /// </summary>
        public decimal IvaGuias { get; set; }
    }

    /// <summary>
    /// Metodo de pago del SAT
    /// </summary>
    public class MetodoPagoSAT
    {
        [Key]
        public int IdRegistro { get; set; }
        [Index(IsUnique = true)]
        public string Clave { get; set; }

        public string Descripcion { get; set; }

        /// <summary>
        /// Si este método de pago indica que la factura es de crédito
        /// </summary>
        public bool EsCredito { get; set; }
    }

    /// <summary>
    /// Un cliente del sistema
    /// </summary>
    public class Cliente
    {
        public Cliente()
        {
        }

        [Key]
        public int IdRegistro { get; set; }

        public string Nombre { get; set; }

        /// <summary>
        /// Indica si el cliente acepta guías de Flete por cobrar
        /// </summary>
        public bool AceptaFletePorCobrar { get; set; }

        /// <summary>
        /// True si el cliente acepta flete por cobrar en zonas extendidas
        /// </summary>  
        public bool AceptaFxcZe { get; set; }

        /// <summary>
        /// Indica si se agruparán las guías en un solo concepto de factura al facturar las guías
        /// </summary>
        public bool AgruparGuiasAlFacturar { get; set; }

        /// <summary>
        /// Si es true el cliente recibirá correos informando del status de la guía
        /// </summary>
        public bool RecibirCorreoStatusGuia { get; set; }

        /// <summary>
        /// Correo del cliente
        /// </summary>
        public string CorreoCliente { get; set; }

        public DatosFacturarCliente DatosFacturacion { get; set; } = new DatosFacturarCliente();

        /// <summary>
        /// Colonia de la dirección de los datos de facturación
        /// </summary>
        public int? IdDatosFacturacionColonia { get; set; }

        /// <summary>
        /// En caso de que el cliente acepte creditos es la sucursal donde se realizan los viajes de revisión y cobranza
        /// </summary>
        public int? IdSucursalCobranza { get; set; }

        /// <summary>
        /// Colonia de la direccion de cobranza de los datosCredito
        /// </summary>
        public int? IdDatosCreditoColonia { get; set; }

        /// <summary>
        /// Id de vendedor
        /// </summary>
        public int? IdVendedor { get; set; }

        /// <summary>
        /// Tarifa especial que aplica para este cliente. Si es null aplica la tarifa general. Este es el convenio
        /// </summary>
        public int? IdTarifa { get; set; }

        /// <summary>
        /// Es la fecha de inicio del cliente para las comisiones
        /// </summary>
        public DateTimeOffset FechaDeInicioParaComisiones { get; set; }

        /// <summary>
        /// Fecha de creación del registro.
        /// </summary>
        public DateTimeOffset FechaCreacion { get; set; }

      
        /// <summary>
        /// Cuando un paquete sea enviado, el sistema va a validar que la persona que recibe el paquete sea uno de los contactos autorizados. Si no esta habilitada esta opción cualquier persona podrá recibir el paquete del cliente
        /// </summary>
        public bool SoloRecibirContactosAutorizado { get; set; }

        public IdentificacionCliente IdentificacionCliente { get; set; } = new IdentificacionCliente();
        public DatosComercialCliente DatosComercialCliente { get; set; } = new DatosComercialCliente();

        /// <summary>
        /// Datos de cobranza
        /// </summary>
        public DatosCredito DatosCredito { get; set; } = new DatosCredito();

        /// <summary>
        /// Telefono del cliente
        /// </summary>
        public string Telefono { get; set; }

        /// <summary>
        /// Si es true este cliente solo podrá ver las descripciones de paquete que tiene relacionadas
        /// </summary>
        public bool VerSoloDescripcionesPaquetesRelacionados { get; set; }

        /// <summary>
        /// si es true esté cliente será visible unicamente para los clientes que lo tengan relacionado
        /// </summary>
        public bool VisibleExclusivoClientesRelacionados { get; set; }

        /// <summary>
        /// si s true este cliente únicamente podrá ver los clientes que tiene relacionado y a si mismo
        /// </summary>
        public bool VerSoloClientesRelacionados { get; set; }


    }


    public enum GiroEmpresa
    {
        AguaPotable,
        BienesRaices,
        Cinematrografia,
        Cobranza,
        Comercial,
        Construccion,
        Derecho,
        DeServicio,
        Editorial,
        Electricidad,
        EmpresaDiseño,
        Industrial,
        Ganaderia,
        Metalurgia,
        MercadoMayorista,
        Mineria,
        Pesca,
        ProductoresAgricolas,
        Software,
        Transporte,
        Telecomunicaciones,
        Turismo,
        Vigilancia
    }

    public enum Clasificacion
    {
        A,
        AA,
        AAA
    }

    public enum DiaSemana
    {
        Lunes,
        Martes,
        Miercoles,
        Jueves,
        Viernes,
        Sabado,
        Domingo
    }


    /// <summary>
    /// Forma de pago del movimiento de caja.
    /// </summary>
    public enum FormaPagoMovimientoCaja
    {
        Efectivo = 0,
        Cheque = 1,
        Transferencia = 2,
        TarjetaCredito = 3,
        TarjetaDebito = 4,
        NotaCredito = 5,
        NotaCargo = 6,
    }

    [ComplexType]
    public class DatosCredito
    {
        /// <summary>
        /// Dia establecido para la salida a revisión
        /// </summary>
        public DiaSemana? DiaRevision { get; set; }
        /// <summary>
        /// Dia establecido para la salida a cobranza
        /// </summary>
        public DiaSemana? DiaCobranza { get; set; }

        /// <summary>
        /// Forma de pago en el cobro de credito. Esta es la forma de pago que van a tomar las facturas de cobranza y los movimientos de caja de los cobros
        /// </summary>
        public FormaPagoMovimientoCaja? FormaPago { get; set; }

        /// <summary>
        /// Limite de crédito del cliente, es nulo si no tiene credito
        /// </summary>
        public decimal? LimiteCredito { get; set; }

        /// <summary>
        /// Cantidad de dias que se le da al cliente para pagar sus guias
        /// </summary>
        public int? DiasCredito { get; set; }

        /// <summary>
        /// Direccion de cobranza del cliente
        /// </summary>
        public Direccion DireccionCobranza { get; set; } = new Direccion();

        /// <summary>
        /// Si es true significa que esté cliente tiene bloqueada su linea de crédito
        /// </summary>
        public bool CreditoBloqueado { get; set; }

    }


    /// <summary>
    /// Datos de facturación de un cliente
    /// </summary>
    [ComplexType]
    public class DatosFacturarCliente
    {
        public Direccion Direccion { get; set; } = new Direccion();
        public string CorreoFacturacion { get; set; }

        /// <summary>
        /// Atributo condicional para expresar el número de registro de identidad fiscal del receptor cuando sea residente en el extranjero. Es requerido cuando se incluya el complemento de comercio exterior.
        /// </summary>
        public string NumeroRegistroIdentidadFiscal { get; set; }

        /// <summary>
        /// Atributo condicional para registrar la clave del país de residencia para efectos fiscales del receptor del comprobante, cuando se trate de un extranjero, y que es conforme con la especificación ISO 3166-1 alpha-3. Es requerido cuando se incluya el complemento de comercio exterior o se registre el atributo NumRegIdTrib.
        /// </summary>
        public string ResidenciaFiscal { get; set; }

        /// <summary>
        /// Numero de cuenta ordenante el cual se va a tomar al crear facturas de este cliente
        /// </summary>
        public string NoCuentaOrdenante { get; set; }
    }

    /// <summary>
    /// Identificación de una persona. Un cliente o un contacto debe de tener por lo menos uno de estos campos asignados y estos campos deben de ser unicos entre si
    /// </summary>
    [ComplexType]
    public class IdentificacionCliente
    {
        public IdentificacionCliente() { }
        public IdentificacionCliente(string ine, TipoPersona tipoPersona, string rfc, string curp, string pasaporte)
        {
            Ine = ine;
            TipoPersona = tipoPersona;
            Rfc = rfc;
            Curp = curp;
            Pasaporte = pasaporte;
        }

        /// <summary>
        /// Numero del INE/IFE
        [Index(IsUnique = true)]
        public string Ine { get; set; }

        /// <summary>
        /// Tipo de persona
        /// </summary>
        public TipoPersona TipoPersona { get; set; }

        /// <summary>
        /// Numero de RFC
        /// </summary>
        [Index]
        public string Rfc { get; set; }

        /// <summary>
        /// Numero de CURP
        /// </summary>
        [Index(IsUnique = true)]
        public string Curp { get; set; }

        /// <summary>
        /// Numero de pasaporte
        /// </summary>
        [Index(IsUnique = true)]
        public string Pasaporte { get; set; }
    }

    /// <summary>
    /// Información comercial de un cliente. Estos campos serán utilizados para distintos reportes estadísticos.
    /// </summary>
    [ComplexType]
    public class DatosComercialCliente
    {
        public DatosComercialCliente() { }
        public DatosComercialCliente(int potencialEnvioPkts, int potencialRecepcionPkts, decimal potencialEnvioDinero, decimal potencialRecepcionDinero, GiroEmpresa? giroEmpresa, Clasificacion? clasificacion)
        {
            GiroEmpresa = giroEmpresa;
            Clasificacion = clasificacion;
        }

        /// <summary>
        /// Valor que indica el potencial de paquetes enviados por mes de este cliente
        /// </summary>
        public int PotencialEnvioPkts { get; set; }

        /// <summary>
        /// Valor que indica el potencial de paquetes recibidos por mes de este cliente
        /// </summary>
        public int PotencialRecepcionPkts { get; set; }

        /// <summary>
        /// Valor que indica el potencial de dinero enviado por mes de este cliente
        /// </summary>
        public int PotencialEnvioDinero { get; set; }

        /// <summary>
        /// Valor que indica el potencial de dinero enviado por mes de este cliente
        /// </summary>
        public decimal PotencialRecepcionDinero { get; set; }

        /// <summary>
        /// Giro de la empresa
        /// </summary>
        public GiroEmpresa? GiroEmpresa { get; set; }

        /// <summary>
        /// Clasificacion de la empresa
        /// </summary>
        public Clasificacion? Clasificacion { get; set; }
    }

    /// <summary>
    /// Estatus de la cancelacion
    /// </summary>
    public enum EstatusCancelacion
    {
        /// <summary>
        /// El comprobante fue cancelado exitosamente sin requerir aceptación
        /// </summary>
        CanceladoSinAceptacion,

        /// <summary>
        /// El comprobante fue cancelado aceptando la solicitud de cancelación
        /// </summary>
        CanceladoConAceptacion,

        /// <summary>
        /// El comprobante recibió una solicitud de cancelación y se encuentra en espera de una respuesta o aun no es reflejada
        /// </summary>
        EnProceso,

        /// <summary>
        /// El comprobante no se cancelo porque se rechazo la solicitud de cancelación
        /// </summary>
        Rechazada,

        /// <summary>
        /// El comprobante fue cancelado ya que no se recibió respuesta del receptor en el tiempo límite.
        /// </summary>
        CanceladoPlazoVencido
    }

    /// <summary>
    /// Indica que una factura es una nota de crédito de otra factura.
    /// Note que el SAT permite que una nota de crédito aplique a varias facturas pero en Uruz sólo se va a permitir una nota de crédito a una factura
    /// </summary>
    public class NotaCredito
    {
        public NotaCredito() { }


        /// <summary>
        /// Id de factura que es la nota de crédito (devolución parcial o total).
        /// Note que esta es la llave primaria
        /// </summary>
        [Key]
        [ForeignKey(nameof(FacturaNotaCredito))]
        public int IdFacturaNotaCredito { get; set; }
        public Factura FacturaNotaCredito { get; set; }

        /// <summary>
        /// Id de la factura a la que esta nota de crédito esta aplicando
        /// </summary>
        [ForeignKey(nameof(FacturaAplica))]
        public int IdFacturaAplica { get; set; }
        public Factura FacturaAplica { get; set; }

        /// <summary>
        /// Fecha de la creación de la nota de crédito
        /// </summary>
        public DateTimeOffset FechaCreacion { get; set; }
    }

    /// <summary>
    /// Indica que una factura fue cancelada
    /// </summary>
    public class CancelacionFactura
    {
        public CancelacionFactura() { }
   

        /// <summary>
        /// Id de la factura cancelada, note que esta es la llave primaria ya que una factura sólo se puede cancelar una vez
        /// </summary>
        [Key]
        [ForeignKey(nameof(Factura))]
        public int IdFactura { get; set; }
        public Factura Factura { get; set; }

        /// <summary>
        /// Fecha en la que se canceló la factura
        /// </summary>
        public DateTimeOffset FechaCancelacion { get; set; }

        /// <summary>
        /// Motivo de cancelación de la factura
        /// </summary>
        public string MotivoCancelacion { get; set; }

        /// <summary>
        /// Estatus en el cual se encuentra esta cancelación ante el SAT
        /// </summary>
        public EstatusCancelacion EstatusCancelacion { get; set; }
    }

    /// <summary>
    /// Una sucursal del sistema
    /// </summary>
    public class Sucursal
    {
        public Sucursal() { }

        /// <summary>
        /// Llave primaria
        /// </summary>
        [Key]
        public int IdRegistro { get; set; }

        /// <summary>
        /// Nombre de la sucursal
        /// </summary>
        public string Nombre { get; set; }

        /// <summary>
        /// Dirección de la sucursal
        /// </summary>
        public Direccion Direccion { get; set; } = new Direccion();

        /// <summary>
        /// Codigo IATA de esta sucursal
        /// </summary>
        [Required, RegularExpression("[A-Z]{3}")]
        [Index("IX_Sucursal_IATA_Numero", IsUnique = true, Order = 1)]
        public string Iata { get; set; }

        /// <summary>
        /// Numero de la sucursal, diferentes sucursales con el mismo IATA deben de tener numeros diferente, por ejemplo si hay dos sucursales en CMX, deben de ser CMX-1 y CMX-2
        /// </summary>
        [Index("IX_Sucursal_IATA_Numero", IsUnique = true, Order = 2)]
        public int Numero { get; set; }

        /// <summary>
        /// Colonia de la dirección de la sucursal
        /// </summary>
        public int IdDireccionColonia { get; set; }

        /// <summary>
        /// Serie que se utilizará en las facturas que provengan de está sucursal
        /// </summary>
        [Required, Index(IsUnique = true)]
        public string Serie { get; set; }


        /// <summary>
        /// Es la caja administrativa de la sucursal
        /// </summary>
        public int? IdCajaAdministrativa { get; set; }

        /// <summary>
        /// Clave de esta sucursal para el catálogo contable, ya que toda póliza contable debe de estar relacionada con una sucursal
        /// </summary>
        public int ClaveSistemaContabilidad { get; set; }

        /// <summary>
        /// True para indicar que la sucursal es nueva
        /// </summary>
        public bool EsNueva { get; set; }

        /// <summary>
        /// Certificado .cer
        /// </summary>
        public byte[] Certificado { get; set; }

        /// <summary>
        /// Nombre de archivo certificado .cer con extensión
        /// </summary>
        public string NombreCertificadoConExtension { get; set; }

        /// <summary>
        /// llave .key
        /// </summary>
        public byte[] Llave { get; set; }

        /// <summary>
        /// Nombre de archivo llave .key con extensión
        /// </summary>
        public string NombreLlaveConExtension { get; set; }

        /// <summary>
        /// Contraseña de la llave
        /// </summary>
        public string ContraseñaLlave { get; set; }

        /// <summary>
        /// Tasas de impuestos por sucursal
        /// </summary>
        public TasaImpuestos TasaImpuestos { get; set; } = new TasaImpuestos();

        /// <summary>
        /// Codigo postal 
        /// </summary>
        public string CodigoPostal { get; set; }

    }

    [ComplexType]
    public class TasaImpuestos
    {
        public TasaImpuestos()
        {

        }


        public decimal TasaIva { get; set; }
        public decimal RetencionIvaPersonaFisica { get; set; }
        public decimal RetencionIvaPersonaMoral { get; set; }
    }

    /// <summary>
    /// Indica a que municipios atienden las sucursales
    /// </summary>
    public class SucursalMunicipio
    {
       
        public SucursalMunicipio() { }

        /// <summary>
        /// Sucursal relacionada con el municipio
        /// </summary>
        public int IdSucursal { get; set; }

        /// <summary>
        /// Municipio relacionado
        /// </summary>
        public int IdMunicipio { get; set; }
    }
}
