using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Test.Uruz
{
    public class FacturaDTO : Factura
    {

        /// <summary>
        /// Si esta factura es una corrección este es el nombre de la factura que fue corregida
        /// </summary>
        public string FacturaOrigenCorreccion { get; set; }

        /// <summary>
        /// Nombre del cliente relacionado con la factura
        /// </summary>
        public string NombreCliente { get; set; }
        public string CorreoClienteFacturacion { get; set; }

        /// <summary>
        /// Es true si la factura está cancelada
        /// </summary>
        public bool EsCancelada { get; set; }

        /// <summary>
        /// Es true si la factura es PP y esta pagada, es false si es PPD y no esta pagada o null si no es PPD
        /// </summary>
        public bool? EsPPDPagada { get; set; }

        /// <summary>
        /// Serie y folio de la factura
        /// </summary>
        public string SerieFolio { get; set; }

        /// <summary>
        /// Importe total de la factura
        /// </summary>
        public decimal ImporteTotal { get; set; }

        /// <summary>
        /// Importe subtotal de la factura
        /// </summary>
        public decimal ImporteSubtotal { get; set; }

        /// <summary>
        /// Importe total de IVA de la factura
        /// </summary>
        public decimal TotalIVA { get; set; }

        /// <summary>
        /// Importe total de IEPS de la factura
        /// </summary>
        public decimal TotalIEPS { get; set; }

        public decimal? TotalIVAGuias { get; set; }
        public bool MismosImpuestos { get; set; }

        /// <summary>
        /// Importe total de retención de iva de la factura
        /// </summary>
        public decimal TotalRetencionIVA { get; set; }

        /// <summary>
        /// Es true si la factura está timbrada
        /// </summary>
        public bool EsTimbrada { get; set; }

        /// <summary>
        /// Es true si la factura es una nota de crédito
        /// </summary>
        public bool EsNotaDeCredito { get; set; }

        /// <summary>
        /// Es true si la factura es una nota de cargo
        /// </summary>
        public bool EsNotaDeCargo { get; set; }

        /// <summary>
        /// Es true si la factura proviene de cobranza
        /// </summary>
        public bool EsDeCredito { get; set; }

        /// <summary>
        /// Si la factura tiene un cliente asigaado
        /// </summary>
        public bool TieneCliente { get; set; }

        /// <summary>
        /// Folio de la factura que corrigió esta factura
        /// </summary>
        public string FolioFacturaCorrigio { get; set; }

        /// <summary>
        /// En caso de ser una nota de crédito o de cargo, es el folio de la factura original
        /// </summary>
        public string FolioFacturaOriginal { get; set; }

        /// <summary>
        /// True si esta póliza esta contabilizadas
        /// </summary>
        public bool Contabilizada { get; set; }

        /// <summary>
        /// True si esta poliza fue cancelada y si la cancelación tiene una póliza relacionada
        /// </summary>
        public bool CancelacionContabilizada { get; set; }

        /// <summary>
        /// True si la factura tiene retenciones
        /// </summary>
        public bool TieneRetencionesIVA { get; set; }

        /// <summary>
        /// Nombre de la sucursal de la factura
        /// </summary>
        public string NombreSucursal { get; set; }

        /// <summary>
        /// Nombre de la sucursal de cobranza del cliente
        /// </summary>
        public string NombreSucursalCobranza { get; set; }

        /// <summary>
        /// Id de la sucursal de cobranza
        /// </summary>
        public int? IdSucursalCobranza { get; set; }

        /// <summary>
        /// Iata de la sucursal de cobranza del cliente
        /// </summary>
        public string IataSucursalCobranza { get; set; }

        /// <summary>
        /// Iata de la sucursal a la que hace referencia la factura
        /// </summary>
        public string IataSucursal { get; set; }

        /// <summary>
        /// Método de pago de sat de la factura
        /// </summary>
        public string ClaveMetodoPagoSAT { get; set; }

        /// <summary>
        /// Cuando esta factura es de método de pago ppd representa el total de lo que se ha abonado a la misma
        /// </summary>
        public decimal TotalAbonado { get; set; }

        /// <summary>
        /// Cuando esta factura es de método de pago ppd representa el resto por pagar
        /// </summary>
        public decimal Saldo { get; set; }

        /// <summary>
        /// Fecha en la que se pago la factura 
        /// Si esta no esta liquidada no debe de aparecer en pantalla
        /// </summary>
        public DateTimeOffset? FechaPago { get; set; }

        /// <summary>
        /// Fecha de cancelación de la factura en caso de estar cancelada
        /// </summary>
        public DateTimeOffset? FechaCancelacion { get; set; }
        /// <summary>
        /// Los folios de las notas de crédito que estan siendo aplicados a esta factura, separados por coma
        /// </summary>
        public string FoliosNotaCredito { get; set; }

        /// <summary>
        /// Estatus de la cancelación de la factura
        /// </summary>
        public EstatusCancelacion? EstatusDeCancelacion { get; set; }

        /// <summary>
        /// El numero de parcialidad del ultimo abono de REP aplicado a esta factura
        /// </summary>
        public int? UltNumParcialidadPago { get; set; }

        /// <summary>
        /// REPs que han sido aplicados a esta factura
        /// </summary>
        public string FolioReps { get; set; }
    }

}
