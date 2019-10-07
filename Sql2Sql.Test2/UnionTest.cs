using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sql2Sql.Tests;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sql2Sql.Test
{
    [TestClass]
    public class UnionTest
    {
        [TestMethod]
        public void SimplePostUnion()
        {
            var q = Sql
                .From<Cliente>()
                .Select(x => new
                {
                    name = "first"
                })
                .OrderBy(x => x.Precio)
                .Limit(1)
                .Union(
                    Sql
                    .From<Cliente>()
                    .Select(x => new
                    {
                        name = "second"
                    })
                )
                .OrderBy(x => x.Nombre)
                ;
            var actual = q.ToString();

        }
    }
}
