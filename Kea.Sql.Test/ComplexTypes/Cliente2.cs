using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Test.ComplexTypes
{
    public class Direccion
    {
        public Direccion(int numero, string calle)
        {
            Numero = numero;
            Calle = calle;
        }

        public int Numero { get; }
        public string Calle { get; }
        /// <summary>
        /// Note que esta propiedad no esta en el constructor, aún así se debe de inicializar correctamente
        /// </summary>
        public string Colonia { get; set; }
    }
    /// <summary>
    /// Una entidad con propiedades de navegación recursivas
    /// </summary>
    public class Cliente2
    {
        public Cliente2(string nombre)
        {
            this.Nombre = nombre;
        }

        public string Nombre { get; }
        public Direccion Dir { get; set; }

        public List<Cliente2> Hermanos { get; set; }
        public Cliente2 Esposa { get; set; }
    }
}
