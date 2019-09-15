using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Test.ComplexTypes
{
    /// <summary>
    /// Identificador de una migración
    /// </summary>
    public class MigId
    {
        public MigId(int numero, string nombre)
        {
            Numero = numero;
            Nombre = nombre;
        }

        public int Numero { get; }
        public string Nombre { get; }
    }

    /// <summary>
    /// Un tipo con subtipos complejos que no estan marcados con el atributo [ComplexType]
    /// </summary>
    public class MigracionDb
    {
        public MigracionDb(MigId id)
        {
            Id = id;
        }

        public MigId Id { get; }
    }
}
