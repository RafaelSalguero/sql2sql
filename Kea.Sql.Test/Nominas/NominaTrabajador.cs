using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Test.Nominas
{
    public enum TipoNomina
    {
        Ordinaria,
        Extraordinaria
    }

    /// <summary>
    /// Una nómina generada para un trabajador
    /// </summary>
    public class NominaTrabajador
    {
        public NominaTrabajador() { }
        /// <summary>
        /// Crea una nómina con los datos mínimos, note que falta agregar los datos que faltan con el PegarDatosRelacionFolio
        /// </summary>
        public NominaTrabajador(
            int idRegistroPatronal,
            int idTrabajador,
            string serie,
            TipoNomina tipo,
            TipoNominaActiva tipoNomActiva,
            DateTime fechaPago,
            TipoPeriodo tipoPeriodo,
            DateTime periodoIni,
            DateTime periodoFin,
            int diasTrab,
            int? idSesionNA,
            bool generaAltasBajas,
            DateTime? trabInicio,
            DateTime? trabFinal,
            decimal salarioDiario,
            decimal salarioDiarioIntegrado,
            bool aplicaTablaIsr)
        {
            IdRegistroPatronal = idRegistroPatronal;
            IdTrabajador = idTrabajador;
            Serie = serie;
            Tipo = tipo;
            TipoNomActiva = tipoNomActiva;
            GeneraAltasBajas = generaAltasBajas;
            FechaPago = fechaPago;
            TipoPeriodo = tipoPeriodo;
            PeriodoIni = periodoIni;
            PeriodoFin = periodoFin;
            DiasTrab = diasTrab;
            IdSesionNA = idSesionNA;
            TrabInicio = trabInicio;
            TrabFinal = trabFinal;
            SalarioDiario = salarioDiario;
            SalarioDiarioIntegrado = salarioDiarioIntegrado;
            AplicaTablaIsr = aplicaTablaIsr;
        }

        public int IdRegistro { get; set; }

        #region Redundantes
        //Columnas redundantes, son mantenidas por triggers

        public int? IdSalarioMinimo { get; set; }

        public int? IdTablaIsr { get; set; }
        #endregion


        /// <summary>
        /// Id del registro patronal
        /// </summary>
        public int IdRegistroPatronal { get; set; }

        /// <summary>
        /// Trabajador al que se le esta generando la nómina
        /// </summary>
        public int IdTrabajador { get; set; }

        public string Serie { get; set; }

        public int Folio { get; set; }

        /// <summary>
        /// Si es ordinaria o extraordinaria
        /// </summary>
        public TipoNomina Tipo { get; set; }

        /// <summary>
        /// Tipo de nómina activa de la relación trabajador
        /// </summary>
        public TipoNominaActiva TipoNomActiva { get; set; }

        /// <summary>
        /// Código postal del lugar de expedición
        /// </summary>
        public string CPLugarExp { get; set; }

        /// <summary>
        /// Fecha de creación de la nómina
        /// </summary>
        public DateTime FechaCreacion { get; set; }

        /// <summary>
        /// Fecha en la que el empleador realizó el pago
        /// </summary>
        public DateTime FechaPago { get; set; }

        public TipoPeriodo TipoPeriodo { get; set; }
        public DateTime PeriodoIni { get; set; }
        public DateTime PeriodoFin { get; set; }

        /// <summary>
        /// Fecha en la que dieron de alta al trabajador
        /// </summary>
        public DateTime FechaAlta { get; set; }

        /// <summary>
        /// Id de la sesión que generó esta nómina en caso de que provenga de una generación de nómina aleatoria
        /// </summary>
        public int? IdSesionNA { get; set; }

        /// <summary>
        /// Id de la sesión de nómina administrativa que generó esta nómina
        /// </summary>
        public int? IdSesionNAdm { get; set; }

        /// <summary>
        /// Cantidad de días trabajados, son los que se usan para el calculo de la nómina, note que no tiene relacion con <see cref="TrabInicio"/> y <see cref="TrabFinal"/>
        /// </summary>
        public int DiasTrab { get; set; }

        /// <summary>
        /// True si esta nómina genera movimientos de altas y bajas para las exportaciones a CSV, esto sólo aplica para las nóminas aleatorias ya que los movimientos de las administrativas
        /// provienen de los mov. afil.
        /// </summary>
        public bool GeneraAltasBajas { get; set; }

        /// <summary>
        /// Sólo aplica si <see cref="GeneraAltasBajas"/>, es la fecha de inicio del periodo de alta y baja.
        /// Un CHECK valida que no sea null en este caso
        /// </summary>
        public DateTime? TrabInicio { get; set; }

        /// <summary>
        /// Sólo aplica si <see cref="GeneraAltasBajas"/>, es la fecha final del periodo de alta y baja
        /// Un CHECK valida que no sea null en este caso
        /// </summary>
        public DateTime? TrabFinal { get; set; }

        /// <summary>
        /// Salario diario para esta nómina
        /// </summary>
        public decimal SalarioDiario { get; set; }

        /// <summary>
        /// Salario diario integrado para esta nómina
        /// </summary>
        public decimal SalarioDiarioIntegrado { get; set; }

        /// <summary>
        /// Subsidio causado al empleo
        /// </summary>
        public decimal SubCausado { get; set; }

        /// <summary>
        /// Id del departamento de esta nomina
        /// </summary>
        public int IdDepartamento { get; set; }

        /// <summary>
        /// Id del estado donde trabajó el empleado
        /// </summary>
        public int IdEstadoSAT { get; set; }

        /// <summary>
        /// Id del puesto
        /// </summary>
        public int IdPuesto { get; set; }

        /// <summary>
        /// Si el trabajador esta sindicalizado, esta en la relación del trabajador
        /// </summary>
        public bool Sindicalizado { get; set; }

        /// <summary>
        /// Tipo del trabajador, esta en la relación
        /// </summary>
        public TipoTrabajador TipoTrabajador { get; set; }

        /// <summary>
        /// Tipo de salario, esta en la relación
        /// </summary>
        public TipoSalario TipoSalario { get; set; }

        /// <summary>
        /// Tipo de jornada, esta en la relación
        /// </summary>
        public TipoJornada TipoJornada { get; set; }

        /// <summary>
        /// Clave de la clínica UMF del IMSS, está en la relación
        /// </summary>
        public string ClaveClinicaImss { get; set; }

        /// <summary>
        /// Tipo de crédito infonavit del trabajador al momento de creación de la nómina
        /// </summary>
        public TipoCreditoInfona? TipoCredito { get; set; }

        /// <summary>
        /// Valor del crédito infonavit del trabajador al momento de creación de la nómina
        /// </summary>
        public decimal? ValorCredito { get; set; }

        /// <summary>
        /// Si para esta nómina aplica la tabla de ISR y de subsidios
        /// </summary>
        public bool AplicaTablaIsr { get; set; }

        /// <summary>
        /// Tipo de contrato del trabajador
        /// </summary>
        public int IdTipoContratoSAT { get; set; }

        /// <summary>
        /// Tipo de jornada del trabajador
        /// </summary>
        public int IdTipoJornadaSAT { get; set; }

        /// <summary>
        /// Tipo de regimen del trabajador
        /// </summary>
        public int IdTipoRegimenSAT { get; set; }

        /// <summary>
        /// Id del riesgo del puesto del SAT
        /// </summary>
        public int IdRiesgoPuestoSAT { get; set; }

        /// <summary>
        /// Id del perfil de prestaciones
        /// </summary>
        public int? IdPerfilPercepcion { get; set; }

        /// <summary>
        /// Porcentaje de la prima vacacional
        /// </summary>
        public decimal PrimaVacacional { get; set; }

        //TODO: Debe de haber una tabla de periodicidades para no tener repetida la periodicidad y la periodicidad del SAT:

        /// <summary>
        /// Periodicidad del pago para esta nomina
        /// </summary>
        public int IdPeriodicidadPagoSAT { get; set; }

    }
}
