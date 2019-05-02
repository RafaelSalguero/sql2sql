using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using LinqKit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KeaSql.Test.Uruz
{
    [TestClass]
   public  class UruzTest
    {
        [TestMethod]
        public void FacturaDtoTest()
        {
            Expression<Func< Factura, string>> nombreFactura = x => x.Serie + "-" + x.Folio;
            var filtro = new FiltroFacturas();
            var auxiliar = Sql.FromTable<Factura>()
               .Inner().JoinTable<FacturaView>().OnTuple(x => x.Item1.IdRegistro == x.Item2.IdFactura)
               .Inner().JoinTable<MetodoPagoSAT>().On(x => x.Item1.IdMetodoPagoSAT == x.Item3.IdRegistro)
               .Left().JoinTable<Factura>().On(x => x.Item2.IdNotaCreditoFacturaOriginal == x.Item4.IdRegistro)
               .Left().JoinTable<Factura>().On(x => x.Item2.IdFacturaCorrigio == x.Item5.IdRegistro)
               .Left().JoinTable<Cliente>().On(x => x.Item1.IdCliente == x.Item6.IdRegistro)
               .Alias(x => new
               {
                   f = x.Item1,
                   v = x.Item2,
                   m = x.Item3,
                   fnco = x.Item4,
                   fc = x.Item5,
                   Cliente = x.Item6
               })
               .Select(from => Tonic.LinqEx.CloneSimpleSelector(from, x => x.f, x => new FacturaDTO
               {
                   SerieFolio = nombreFactura.Invoke(x.f),
                   FacturaOrigenCorreccion = nombreFactura.Invoke(x.f.FacturaCorreccionOrigen),
                   NombreCliente = x.Cliente.Nombre,
                   CorreoClienteFacturacion = x.Cliente.DatosFacturacion.CorreoFacturacion,
                   EsCancelada = x.v.Cancelada,
                   EsPPDPagada = x.v.PagadaCredito,
                   EsDeCredito = x.v.EsCredito,
                   TieneCliente = x.f.IdCliente != null,
                   FolioFacturaCorrigio = nombreFactura.Invoke(x.fc),
                   EsNotaDeCargo = x.f.Origen == OrigenFactura.NotaDeCargo,
                   EsTimbrada = x.v.FechaTimbrado != null,
                   ImporteTotal = x.v.Total,
                   ImporteSubtotal = x.v.Subtotal,
                   TotalIVA = x.v.Iva,
                   TotalRetencionIVA = x.v.RetencionIva,
                   TotalIEPS = x.v.Ieps,
                   TotalIVAGuias = x.v.IvaGuias,
                   MismosImpuestos = x.v.Iva + x.v.Ieps == x.v.IvaGuias,
                   Contabilizada = x.v.IdPolizaContable != null,
                   CancelacionContabilizada = x.v.IdPolizaCancelacion != null,
                   TieneRetencionesIVA = x.v.RetencionIva != 0,
                   ClaveMetodoPagoSAT = x.m.Clave,
                   TotalAbonado = x.v.ImporteReps,
                   Saldo = x.v.Saldo,
                   FechaPago = x.v.FechaPago,
                   FechaCancelacion = x.v.FechaCancelacion,
                   FolioFacturaOriginal = x.fnco != null ? nombreFactura.Invoke(x.fnco) : null,
                   FoliosNotaCredito = x.v.FoliosNotasCredito,
                   UltNumParcialidadPago = x.v.UltNumParcialidad,
                   FolioReps = x.v.FolioReps
               })
               .Invoke(from))
               ;

            var auxiliar2 =
                Sql.From(auxiliar)
                .Left().JoinTable<Sucursal>().OnTuple(x => x.Item1.IdSucursalCobranza == x.Item2.IdRegistro)
                .Left().JoinTable<Sucursal>().On(x => x.Item1.IdSucursal == x.Item3.IdRegistro)
                .Left().JoinTable<CancelacionFactura>().On(x => x.Item1.IdRegistro == x.Item4.IdFactura)
                .Left().JoinTable<NotaCredito>().On(x => x.Item1.IdRegistro == x.Item5.IdFacturaNotaCredito)
                .Alias(x => new
                {
                    f = x.Item1,
                    sc = x.Item2,
                    s = x.Item3,
                    cf = x.Item4,
                    nc = x.Item5
                })
                .Select(from => Tonic.LinqEx.CloneSimpleSelector(from, x => x.f, x => new FacturaDTO
                {
                    NombreSucursal = x.s.Nombre,
                    IataSucursal = x.s.Iata,
                    NombreSucursalCobranza = x.sc.Nombre,
                    IataSucursalCobranza = x.sc.Iata,
                    EsNotaDeCredito = x.nc != null,
                    EstatusDeCancelacion = x.cf.EstatusCancelacion
                })
                .Invoke(from))
                ;

            var tabla = Sql.From(auxiliar2)
                .Select(x => x)
                .Where(x =>
                        SqlExpr.ifCond.Invoke(filtro.FechaInicio != null, x.FechaCreacion >= filtro.FechaInicio) &&
                        SqlExpr.ifCond.Invoke(filtro.FechaFinal != null, x.FechaCreacion <= filtro.FechaFinal) &&
                        SqlExpr.ifCond.Invoke(filtro.FechaPagoInicio != null, x.FechaPago >= filtro.FechaPagoInicio) &&
                        SqlExpr.ifCond.Invoke(filtro.FechaPagoFinal != null, x.FechaPago <= filtro.FechaPagoFinal) &&
                        SqlExpr.ifCond.Invoke(filtro.EsNotaCredito != null, x.EsNotaDeCredito == filtro.EsNotaCredito) &&
                        SqlExpr.ifCond.Invoke(filtro.EsNotaCargo != null, x.EsNotaDeCargo == filtro.EsNotaCargo) &&
                        SqlExpr.ifCond.Invoke(filtro.EsTimbrada != null, x.EsTimbrada == filtro.EsTimbrada) &&
                        SqlExpr.ifCond.Invoke(filtro.EsCancelada != null, x.EsCancelada == filtro.EsCancelada) &&
                        SqlExpr.ifCond.Invoke(filtro.DeCredito != null, x.EsDeCredito == filtro.DeCredito) &&
                        SqlExpr.ifCond.Invoke(filtro.TieneCliente != null, x.TieneCliente == filtro.TieneCliente) &&
                        SqlExpr.ifCond.Invoke(filtro.IdSucursalCobranza != null, x.IdSucursalCobranza == filtro.IdSucursalCobranza) &&
                        SqlExpr.ifCond.Invoke(filtro.IdSucursal != null, x.IdSucursal == filtro.IdSucursal) &&
                        SqlExpr.ifCond.Invoke(filtro.IdCliente != null, x.IdCliente == filtro.IdCliente) &&
                        SqlExpr.ifCond.Invoke(filtro.Pagada != null, x.EsPPDPagada == filtro.Pagada) &&
                        SqlExpr.containsStr.Invoke(x.SerieFolio, filtro.SerieFolio)
                       //SqlExpr.ifCond.Invoke(filtro.Origen != null, x.Origen == filtro.Origen) &&
                       //SqlExpr.ifCond.Invoke(filtro.Ids != null && filtro.Ids.Any(), filtro.Ids.Contains(x.IdRegistro))
                       //(!(filtro.IdViajeCobranza != null) || x.ViajeCobranza.Any(viaje => viaje.IdViajeCobranza == filtro.IdViajeCobranza))
                       //Agregar el Any y el contains de arrays
                       )
                       .Limit(filtro.Limite ?? 100)
                       ;

            var queryGeneradoTexto = tabla.ToString();


        }
    }
}
