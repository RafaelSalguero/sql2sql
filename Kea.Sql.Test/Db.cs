using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Tests
{
    public class Estado {
        public int IdRegistro {get;set;}
    }

    public enum TipoPersona
    {
        Fisica,
        Moral,
    }

    public class Cliente
    {
        public int IdEstado {get; set;}
        public int IdRegistro { get; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public TipoPersona Tipo { get; set; }
        public Direccion Dir { get; set; } 
    }

    [ComplexType]
    public class Direccion
    {
        public string Calle { get; set; }
        public DatosPersonales Personales { get; set; } 
    }

    [ComplexType]
    public class DatosPersonales
    {
        public string Telefono { get; set; }
    }

    public class Pago
    {
        public int IdRegistro { get; }
        public decimal Cantidad { get; }
        public int IdCliente { get; }
    }

    public class Factura
    {
        public int IdRegistro { get; set; }
        public int IdCliente { get; set; }
        public int Folio { get; set; }
        public string Serie { get; set; }
    }

    public class Pedido {
        public int IdRegistro { get; }
        public int Folio { get; }
        public string Serie { get; }
    }

    public class ConceptoFactura {
        public int IdRegistro {get;}
        public int IdFactura {get;}
        public decimal Precio {get;}
        public decimal Cantidad {get;}
    }

    public class Db
    {
        public IQueryable<Cliente> Cliente { get; }
        public IQueryable<Estado> Estado { get; }
        public IQueryable<Pago> Pago { get; }
        public IQueryable<Factura> Factura { get; }
        public IQueryable<Pedido> Pedido { get; }
        public IQueryable<ConceptoFactura> ConceptoFactura { get; }
    }
}
