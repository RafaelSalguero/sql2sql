using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Test
{
    public class StatusCajaRutaView
    {
        public int IdCajaRuta { get; set; }
        public DateTimeOffset Fecha { get; set; }
    }
    public class CajaRuta
    {
        public int IdRegistro { get; set; }
    }
    public class DbAzymo
    {
        public IQueryable<StatusCajaRutaView> StatusCajaRutaView { get; set; }
        public IQueryable<CajaRuta> CajaRuta { get; set; }
    }
}
