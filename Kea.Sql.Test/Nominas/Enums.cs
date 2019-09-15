using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Test.Nominas
{
    public enum Periodo
    {
        SEM = 0,
        CAT = 1,
        QUIN = 2,
        MEN = 3,
        BIM = 4
    }

    public enum TipoNominaActiva
    {
        /// <summary>
        /// Nomina aleatoria / eventual de campo
        /// </summary>
        TEC = 0,
        /// <summary>
        /// Nómina administrativa
        /// </summary>
        ADMON = 1,
    }

    public enum TipoTrabajador
    {
        Permanente = 1,
        EventualCiudad = 2,
        EventualConstruccion = 3,
        EventualCampo = 4
    }

    public enum TipoSalario
    {
        Fijo = 0,
        Variable = 1,
        Mixto = 2,
    }

    public enum TipoJornada
    {
        JornadaCompleta = 0,
        UnDiaTrabajado = 1,
        DosDiasTrabajados = 2,
        TresDiasTrabajados = 3,
        CuatroDiasTrabajados = 4,
        CincoDiasTrabajados = 5,
        MenosUnDiaTrabajado = 6
    }


    public enum Sexo
    {
        M,
        F
    }

    public enum TipoCreditoInfona
    {
        Porcentaje = 1,
        CuotaFija = 2,
        VecesSalarioMinimo = 3
    }
}
