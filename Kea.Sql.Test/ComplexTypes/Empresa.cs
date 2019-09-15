using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Test.ComplexTypes
{
    public class Cuenta
    {
        public int IdRegistro { get; set; }
        public string Nombre { get; set; }
    }

    [ComplexType]
    public class EmpresaDatos
    {
        public string Tipo { get; set; }
        public string Rfc { get; set; }
    }

    /// <summary>
    /// Una empresa, hija de un grupo de empresas y relacionada con una cuenta
    /// </summary>
    public class Empresa
    {
        public int IdRegistro { get; set; }

        /// <summary>
        /// Si este registro esta destacado por el usuario
        /// </summary>
        public bool Destacado { get; set; }

        [ForeignKey(nameof(Cuenta))]
        public int IdCuenta { get; set; }
        public Cuenta Cuenta { get; set; }

        public DateTimeOffset FechaMod { get; set; }

        public string Nombre { get; set; }

        /// <summary>
        /// RFC de la empresa
        /// </summary>
        public EmpresaDatos Datos { get; set; }
    }
}
