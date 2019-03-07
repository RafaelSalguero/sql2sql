using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlToSql.Fluent
{
    public static  class STest
    {
        public static void Test()
        {
            Sql2
                .From(new SqlTable<Cliente>())
                .Join(new SqlTable<Estado>()).On(x => x.Item1.IdEstado == x.Item2.IdRegistro)
                .Join(new SqlTable<Factura>()).On(x => x.Item1.IdRegistro == x.Item3.IdCliente)
                .Join(new SqlTable<ConceptoFactura>()).On(x => x.Item3.IdRegistro == x.Item4.IdFactura )
               
                ;
        }
    }
}
