using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlToSql.Test.Nominas
{
    /// <summary>
    /// Indica que a partir de cierta fecha se van a usar ciertas tarifas y subsidios de ISR
    /// </summary>
    public class TablaIsr
    {
        public int IdRegistro { get; set; }

        /// <summary>
        /// Fecha en la que aplica esta tabla de ISR
        /// </summary>
        public DateTime Fecha { get; set; }
    }

    /// <summary>
    /// Tabla de los subsidios del ISR
    /// </summary>
    public class TarifaISR
    {
        public int IdRegistro { get; set; }

        public int IdTablaIsr { get; set; }

        /// <summary>
        /// Límite inferior de la base para que aplique la base
        /// </summary>
        public decimal LimiteInf { get; set; }

        public decimal CuotaFija { get; set; }

        /// <summary>
        /// Porcentaje que aplica sobre el excedente del límite inferior
        /// </summary>
        public decimal PorApliEx { get; set; }
    }

    /// <summary>
    /// Tabla del subsidio al empleo
    /// </summary>
    public class SubsidioEmp
    {
        public int IdRegistro { get; set; }

        public int IdTablaIsr { get; set; }

        /// <summary>
        /// Límite inferior de la base
        /// </summary>
        public decimal LimiteInf { get; set; }

        /// <summary>
        /// Cuota fija
        /// </summary>
        public decimal CuotaFija { get; set; }
    }
}
