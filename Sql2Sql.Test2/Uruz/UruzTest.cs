using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using LinqKit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sql2Sql.Test.Uruz
{
    [TestClass]
   public  class UruzTest
    {
        [TestMethod]
        public void FacturaDtoTest()
        {
            Expression<Func< Factura, string>> nombreFactura = x => x.Serie + "-" + x.Folio;
            var filtro = new FiltroFacturas();
            filtro.Origen = OrigenFactura.Manual;

            var auxiliar = Sql.From<Factura>()
               .Inner().Join<FacturaView>().On(x => x.Item1.IdRegistro == x.Item2.IdFactura)
               .Inner().Join<MetodoPagoSAT>().On(x => x.Item1.IdMetodoPagoSAT == x.Item3.IdRegistro)
               .Left().Join<Factura>().On(x => x.Item2.IdNotaCreditoFacturaOriginal == x.Item4.IdRegistro)
               .Left().Join<Factura>().On(x => x.Item2.IdFacturaCorrigio == x.Item5.IdRegistro)
               .Left().Join<Cliente>().On(x => x.Item1.IdCliente == x.Item6.IdRegistro)
               .Alias(x => new
               {
                   f = x.Item1,
                   v = x.Item2,
                   m = x.Item3,
                   fnco = x.Item4,
                   fc = x.Item5,
                   Cliente = x.Item6
               })
               .Select(x => Sql.Star(x.f).Map(new FacturaDTO
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
               }))
               ;


            var auxiliar2 =
                Sql.From(auxiliar)
                .Left().Join<Sucursal>().On(x => x.Item1.IdSucursalCobranza == x.Item2.IdRegistro)
                .Left().Join<Sucursal>().On(x => x.Item1.IdSucursal == x.Item3.IdRegistro)
                .Left().Join<CancelacionFactura>().On(x => x.Item1.IdRegistro == x.Item4.IdFactura)
                .Left().Join<NotaCredito>().On(x => x.Item1.IdRegistro == x.Item5.IdFacturaNotaCredito)
                .Alias(x => new
                {
                    f = x.Item1,
                    sc = x.Item2,
                    s = x.Item3,
                    cf = x.Item4,
                    nc = x.Item5
                })
                .Select(x => Sql.Star(x.f).Map( new FacturaDTO { 
                    NombreSucursal = x.s.Nombre,
                    IataSucursal = x.s.Iata,
                    NombreSucursalCobranza = x.sc.Nombre,
                    IataSucursalCobranza = x.sc.Iata,
                    EsNotaDeCredito = x.nc != null,
                    EstatusDeCancelacion = x.cf.EstatusCancelacion
                }))
                ;


            var tabla = Sql.From(auxiliar2)
                .Select(x => x)
                .Where(x =>
                        SqlExpr.IfCond.Invoke(filtro.FechaInicio != null, x.FechaCreacion >= filtro.FechaInicio) &&
                        SqlExpr.IfCond.Invoke(filtro.FechaFinal != null, x.FechaCreacion <= filtro.FechaFinal) &&
                        SqlExpr.IfCond.Invoke(filtro.FechaPagoInicio != null, x.FechaPago >= filtro.FechaPagoInicio) &&
                        SqlExpr.IfCond.Invoke(filtro.FechaPagoFinal != null, x.FechaPago <= filtro.FechaPagoFinal) &&
                        SqlExpr.IfCond.Invoke(filtro.EsNotaCredito != null, x.EsNotaDeCredito == filtro.EsNotaCredito) &&
                        SqlExpr.IfCond.Invoke(filtro.EsNotaCargo != null, x.EsNotaDeCargo == filtro.EsNotaCargo) &&
                        SqlExpr.IfCond.Invoke(filtro.EsTimbrada != null, x.EsTimbrada == filtro.EsTimbrada) &&
                        SqlExpr.IfCond.Invoke(filtro.EsCancelada != null, x.EsCancelada == filtro.EsCancelada) &&
                        SqlExpr.IfCond.Invoke(filtro.DeCredito != null, x.EsDeCredito == filtro.DeCredito) &&
                        SqlExpr.IfCond.Invoke(filtro.TieneCliente != null, x.TieneCliente == filtro.TieneCliente) &&
                        SqlExpr.IfCond.Invoke(filtro.IdSucursalCobranza != null, x.IdSucursalCobranza == filtro.IdSucursalCobranza) &&
                        SqlExpr.IfCond.Invoke(filtro.IdSucursal != null, x.IdSucursal == filtro.IdSucursal) &&
                        SqlExpr.IfCond.Invoke(filtro.IdCliente != null, x.IdCliente == filtro.IdCliente) &&
                        SqlExpr.IfCond.Invoke(filtro.Pagada != null, x.EsPPDPagada == filtro.Pagada) &&
                        SqlExpr.ContainsStr.Invoke(x.SerieFolio, filtro.SerieFolio) &&
                        SqlExpr.IfCond.Invoke(filtro.Origen != null, x.Origen == filtro.Origen) 
                       //SqlExpr.ifCond.Invoke(filtro.Ids != null && filtro.Ids.Any(), filtro.Ids.Contains(x.IdRegistro))
                       //(!(filtro.IdViajeCobranza != null) || x.ViajeCobranza.Any(viaje => viaje.IdViajeCobranza == filtro.IdViajeCobranza))
                       //Agregar el Any y el contains de arrays
                       )
                       .Limit(filtro.Limite ?? 100)
                       ;

            var queryGeneradoTexto = tabla.ToString();
            var expected = @"
 SELECT 
    ""x"".*
FROM (
    SELECT 
        ""f"".*,
        ""s"".""Nombre"" AS ""NombreSucursal"", 
        ""s"".""Iata"" AS ""IataSucursal"", 
        ""sc"".""Nombre"" AS ""NombreSucursalCobranza"", 
        ""sc"".""Iata"" AS ""IataSucursalCobranza"", 
        (""nc"".* IS NOT NULL) AS ""EsNotaDeCredito"", 
        ""cf"".""EstatusCancelacion"" AS ""EstatusDeCancelacion""
    FROM (
        SELECT 
            ""f"".*,
            ((""f"".""Serie"" || '-') || ""f"".""Folio"") AS ""SerieFolio"", 
            ((""f"".""Serie"" || '-') || ""f"".""Folio"") AS ""FacturaOrigenCorreccion"", 
            ""Cliente"".""Nombre"" AS ""NombreCliente"", 
            ""Cliente"".""DatosFacturacion_CorreoFacturacion"" AS ""CorreoClienteFacturacion"", 
            ""v"".""Cancelada"" AS ""EsCancelada"", 
            ""v"".""PagadaCredito"" AS ""EsPPDPagada"", 
            ""v"".""EsCredito"" AS ""EsDeCredito"", 
            (""f"".""IdCliente"" IS NOT NULL) AS ""TieneCliente"", 
            ((""fc"".""Serie"" || '-') || ""fc"".""Folio"") AS ""FolioFacturaCorrigio"", 
            (""f"".""Origen"" = 12) AS ""EsNotaDeCargo"", 
            (""v"".""FechaTimbrado"" IS NOT NULL) AS ""EsTimbrada"", 
            ""v"".""Total"" AS ""ImporteTotal"", 
            ""v"".""Subtotal"" AS ""ImporteSubtotal"", 
            ""v"".""Iva"" AS ""TotalIVA"", 
            ""v"".""RetencionIva"" AS ""TotalRetencionIVA"", 
            ""v"".""Ieps"" AS ""TotalIEPS"", 
            ""v"".""IvaGuias"" AS ""TotalIVAGuias"", 
            ((""v"".""Iva"" + ""v"".""Ieps"") = ""v"".""IvaGuias"") AS ""MismosImpuestos"", 
            (""v"".""IdPolizaContable"" IS NOT NULL) AS ""Contabilizada"", 
            (""v"".""IdPolizaCancelacion"" IS NOT NULL) AS ""CancelacionContabilizada"", 
            (""v"".""RetencionIva"" != 0) AS ""TieneRetencionesIVA"", 
            ""m"".""Clave"" AS ""ClaveMetodoPagoSAT"", 
            ""v"".""ImporteReps"" AS ""TotalAbonado"", 
            ""v"".""Saldo"" AS ""Saldo"", 
            ""v"".""FechaPago"" AS ""FechaPago"", 
            ""v"".""FechaCancelacion"" AS ""FechaCancelacion"", 
                
                CASE
                    WHEN (""fnco"".* IS NOT NULL) THEN ((""fnco"".""Serie"" || '-') || ""fnco"".""Folio"")
                    ELSE NULL
                END AS ""FolioFacturaOriginal"", 
            ""v"".""FoliosNotasCredito"" AS ""FoliosNotaCredito"", 
            ""v"".""UltNumParcialidad"" AS ""UltNumParcialidadPago"", 
            ""v"".""FolioReps"" AS ""FolioReps""
        FROM ""Factura"" ""f""
        JOIN ""FacturaView"" ""v"" ON (""f"".""IdRegistro"" = ""v"".""IdFactura"")
        JOIN ""MetodoPagoSAT"" ""m"" ON (""f"".""IdMetodoPagoSAT"" = ""m"".""IdRegistro"")
        LEFT JOIN ""Factura"" ""fnco"" ON (""v"".""IdNotaCreditoFacturaOriginal"" = ""fnco"".""IdRegistro"")
        LEFT JOIN ""Factura"" ""fc"" ON (""v"".""IdFacturaCorrigio"" = ""fc"".""IdRegistro"")
        LEFT JOIN ""Cliente"" ""Cliente"" ON (""f"".""IdCliente"" = ""Cliente"".""IdRegistro"")
    ) ""f""
    LEFT JOIN ""Sucursal"" ""sc"" ON (""f"".""IdSucursalCobranza"" = ""sc"".""IdRegistro"")
    LEFT JOIN ""Sucursal"" ""s"" ON (""f"".""IdSucursal"" = ""s"".""IdRegistro"")
    LEFT JOIN ""CancelacionFactura"" ""cf"" ON (""f"".""IdRegistro"" = ""cf"".""IdFactura"")
    LEFT JOIN ""NotaCredito"" ""nc"" ON (""f"".""IdRegistro"" = ""nc"".""IdFacturaNotaCredito"")
) ""x""
WHERE (""x"".""Origen"" = 0)
LIMIT 100
";
            AssertSql.AreEqual(expected, queryGeneradoTexto);
        }
    }
}
