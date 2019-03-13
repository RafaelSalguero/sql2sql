using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlToSql.Test.Nominas
{
    public class NominaView
    {
        public int IdNominaTrabajador { get; set; }

        public int IdTrabajador { get; set; }

        public string NombreTrabajador { get; set; }
        public string CurpTrabajador { get; set; }
        public string RfcTrabajador { get; set; }
        public string NssTrabajador { get; set; }

        public int IdRegistroPatronal { get; set; }

        /// <summary>
        /// Número del registro patronal
        /// </summary>
        public string NoRegPat { get; set; }

        public int IdEmpresa { get; set; }

        public string NombreEmpresa { get; set; }
        public string RfcEmpresa { get; set; }
        public string CurpEmpresa { get; set; }

        public DateTime PeriodoIni { get; set; }
        public DateTime PeriodoFin { get; set; }
        public TipoPeriodo TipoPeriodo { get; set; }

        /// <summary>
        /// Días trabajados, no son los días del periodo
        /// </summary>
        public int DiasTrab { get; set; }

        public decimal Sueldo { get; set; }
        public decimal Destajo { get; set; }
        public decimal Vacaciones { get; set; }
        public decimal PrimaVacacional { get; set; }

        public decimal Aguinaldo { get; set; }
        public decimal Percepciones { get; set; }

        /// <summary>
        /// Subsidio al empleo causado
        /// </summary>
        public decimal SubCausado { get; set; }

        /// <summary>
        /// Subsidio al empleo entregado
        /// </summary>
        public decimal SubsidioEmpleo { get; set; }
        public decimal OtrosPagos { get; set; }

        public decimal ISR { get; set; }
        public decimal RetImss { get; set; }
        public decimal Prestamos { get; set; }
        public decimal Amort { get; set; }
        public decimal CuotaSind { get; set; }
        public decimal Deducciones { get; set; }
        public decimal Total { get; set; }
        public decimal PercGravado { get; set; }
        public decimal PercExento { get; set; }
        /// <summary>
        /// Deducción del abono al crédito infonavit, note que aquí no se incluyen los 15 pesos del seguro de la vivienda
        /// </summary>
        public decimal Infonavit { get; set; }

        /// <summary>
        /// 15 pesos bimestrales del seguro de la vivienda
        /// </summary>
        public decimal SeguroVivInfona { get; set; }

        /// <summary>
        /// ISR antes del subsidoo
        /// </summary>
        public decimal IsrAntesSub { get; set; }

        /// <summary>
        /// ISR neto, puede ser negativo en caso de que el subsidio sea mayor al ISR calculado
        /// </summary>
        public decimal IsrONeg { get; set; }

        /// <summary>
        /// UUID del timbrado
        /// </summary>
        public string UUID { get; set; }

        /// <summary>
        /// Id del timbrado no cancelado de esta nómina
        /// </summary>
        public int? IdTimbrado { get; set; }

        /// <summary>
        /// Cantidad de días del bimestre para considerar en los calculos del crédito de infonavit, puede provenir ya sea de la configuración del registro patronal o de los días naturales del bimestre
        /// </summary>
        public int DiasBimInfona { get; set; }

        /// <summary>
        /// Fecha de inicio del bimestre
        /// </summary>
        public DateTime IniBim { get; set; }

        /// <summary>
        /// Días de incapacidad
        /// </summary>
        public int DiasIncap { get; set; }

        /// <summary>
        /// Días de vacaciones en total
        /// </summary>
        public int DiasVac { get; set; }

        /// <summary>
        /// Días de vacaciones del tipo "Pagada"
        /// </summary>
        public int DiasVacPagados { get; set; }

        /// <summary>
        /// Días de vacaciones del tipo "Disfrutada"
        /// </summary>
        public int DiasVacDisfrutados { get; set; }
    }
}
