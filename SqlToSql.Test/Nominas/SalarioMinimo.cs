using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Test.Nominas
{
    /// <summary>
    /// Histórico de los salarios mínimos
    /// </summary>
    public class SalarioMinimo
    {
        public int IdRegistro { get; set; }
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Salario minimo
        /// </summary>
        public decimal SalarioMin { get; set; }
        /// <summary>
        /// Valor de la UMA
        /// </summary>
        public decimal UMA { get; set; }
        /// <summary>
        /// Salario de referencia anual
        /// </summary>
        public decimal SalarioRefAnual { get; set; }

        /// <summary>
        /// UMI, se usa para los créditos infonavit
        /// </summary>
        public decimal UMI { get; set; }

        /// <summary>
        /// Valor del factor del decreto
        /// </summary>
        public decimal FactorDecreto { get; set; }

        /// <summary>
        /// Valor del factor de integración
        /// </summary>
        public decimal FactorIntegracion { get; set; }

        /// <summary>
        /// Porcentaje del IMSS
        /// </summary>
        public decimal PorcIMSS { get; set; }

        /// <summary>
        /// Porcentaje ISR facilidad administrativa
        /// </summary>
        public decimal PorcISRAdmin { get; set; }
    }
}
