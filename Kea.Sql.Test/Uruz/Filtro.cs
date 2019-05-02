using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Test.Uruz
{
    public class FiltroFacturas
    {

        public IReadOnlyList<int> Ids { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinal { get; set; }
        public DateTime? FechaPagoInicio { get; set; }
        public DateTime? FechaPagoFinal { get; set; }
        public string SerieFolio { get; set; }
        public int? IdSucursal { get; set; }
        public int? IdCliente { get; set; }
        public OrigenFactura? Origen { get; set; }
        public bool? EsNotaCredito { get; set; }
        public bool? EsNotaCargo { get; set; }
        public bool? EsTimbrada { get; set; }
        public bool? EsCancelada { get; set; }
        public bool? EsREP { get; set; }
        public int? Limite { get; set; }
        public int? IdSucursalCobranza { get; set; }
        public bool? DeCredito { get; set; }
        public bool? TieneCliente { get; set; }
        public bool? Pagada { get; set; }
        public int? IdViajeCobranza { get; set; }
    }
}
