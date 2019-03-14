using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Test.Nominas
{
    public enum TransmisionMovimientos
    {
        IDSE = 0,
        LISTAS = 1,
    }

    /// <summary>
    /// Clase para guardar los registros patronales
    /// </summary>
    public class RegistroPatronal
    {
        public int IdRegistro { get; set; }

        /// <summary>
        /// Nombre del registro patronal
        /// </summary>
        public string Nombre { get; set; }

        /// <summary>
        /// Elevación de la tarifa diaria del ISR para dejarla como mensual, las dos opciones son 30 o 30.4
        /// </summary>
        public decimal ElevacionTarifa { get; set; }

        /// <summary>
        /// Días considerados para el bimestre de infonavit, si no se establece los días del bimestre se calculan para cada nomina según el bimestre que corresponde
        /// </summary>
        public int? DiasCredInfonavit { get; set; }

        /// <summary>
        /// Serie que se usara en la generación de las nominas administrativas ordinarias
        /// </summary>
        public string SerieOrd { get; set; }

        /// <summary>
        /// Serie que se usará en la generación de las nóminas administrativas extraordinarias
        /// </summary>
        public string SerieExt { get; set; }

        /// <summary>
        /// Clave del registro patronal
        /// </summary>
        public string Clave { get; set; }

        /// <summary>
        /// Id de la empresa a la que el registro patronal pertenece
        /// </summary>
        public int IdEmpresa { get; set; }
      

        /// <summary>
        /// Tipo de transmisión de movimientos
        /// </summary>
        public TransmisionMovimientos TransmisionMovimientos { get; set; }

        /// <summary>
        /// Número de guía
        /// </summary>
        public string NumGuia1 { get; set; }

        /// <summary>
        /// /Número de guía 02
        /// </summary>
        public string NumGuia2 { get; set; }

        /// <summary>
        /// Indica si la tabla de tarifas de ISR debe de aplicar para la nomina aleatoria
        /// </summary>
        public bool AplicaTablaIsrAleat { get; set; }

        /// <summary>
        /// Id del riesgo del puesto de este registro patronal
        /// </summary>
        public int IdRiesgoPuestoSAT { get; set; }

        
    }
}
