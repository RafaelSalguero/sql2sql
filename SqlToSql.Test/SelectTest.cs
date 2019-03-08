using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlToSql.Fluent;

namespace SqlToSql.Test
{
    [TestClass]
    public class SelectTest
    {
        [TestMethod]
        public void SimpleSelect()
        {
            var r = Sql2
              .From(new SqlTable<Cliente>())
              .Select(x => new
              {
                  nom = x.Nombre,
                  idEst = x.IdEstado
              });

        }
    }
}
