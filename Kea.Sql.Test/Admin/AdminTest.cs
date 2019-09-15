using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqKit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sql2Sql.Test.Admin
{
    [TestClass]
    public  class AdminTest
    {
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

            public int IdCuenta { get; set; }
            public DateTimeOffset FechaMod { get; set; }

            public int IdUsuarioMod { get; set; }
            public DateTimeOffset FechaCrea { get; set; }
            public int IdUsuarioCrea { get; set; }

            /// <summary>
            /// Id del grupo al que pertenece la empresa
            /// </summary>
            public int IdGrupoEmpresa { get; set; }

            public string Nombre { get; set; }

            /// <summary>
            /// RFC de la empresa
            /// </summary>
            public string Rfc { get; set; }
        }

        /// <summary>
        /// Indica un conjunto de empresas
        /// </summary>
        public class GrupoEmpresa
        {
            public int IdRegistro { get; set; }

            public string Nombre { get; set; }
        }

        /// <summary>
        /// Una cuenta contratada para el uso del sistema
        /// </summary>
        public class Cuenta  
        {
            public int IdRegistro { get; set; }
            public int IdGrupoEmpresa { get; set; }
            public string Nombre { get; set; }
            public string Correo { get; set; }
            public string Telefono { get; set; }
        }

        /// <summary>
        /// Relaciona a los usuarios con las empresas
        /// </summary>
        public class UsuarioEmpresa  
        {
            public int IdRegistro { get; set; }
            public int IdUsuario { get; set; }
            public int IdEmpresa { get; set; }
        }

        public class EmpresaDto : Empresa
        {
            public string NombreGrupo { get; set; }
            public string NombreCuenta { get; set; }
        }

        /// <summary>
        /// Filtros que aplican para todas las consultas
        /// </summary>
        public class FiltroConsulta
        {
            /// <summary>
            /// Búsqueda global por texto
            /// </summary>
            public string Texto { get; set; }

            /// <summary>
            /// Sólo ver registros destacados
            /// </summary>
            public bool Destacado { get; set; }
        }

        /// <summary>
        /// Parametros para realizar una consulta
        /// </summary>
        /// <typeparam name="T">Tipo del filtro</typeparam>
        public class ParametrosConsulta<T>
            where T : FiltroConsulta
        {
            public ParametrosConsulta(T filtro, PaginacionConsulta paginacion)
            {
                Filtro = filtro;
                Paginacion = paginacion;
            }

            public T Filtro { get; }
            public PaginacionConsulta Paginacion { get; }
        }

        /// <summary>
        /// Indica la paginación de una consulta
        /// </summary>
        public class PaginacionConsulta
        {
            /// <summary>
            /// Número de página
            /// </summary>
            public int NoPagina { get; set; }

            /// <summary>
            /// Cantidad de elementos por página, el límite máximo lo define internamente la implementación de la consulta
            /// </summary>
            public int? Limite { get; set; }
        }

        public class EmpresaFiltro : FiltroConsulta
        {
            /// <summary>
            /// Busqueda por RFC
            /// </summary>
            public string Rfc { get; set; }

            /// <summary>
            /// Nombre de la empresa
            /// </summary>
            public string Nombre { get; set; }
        }

        [TestMethod]
        public void EmpresaQueryTest()
        {
            ParametrosConsulta<EmpresaFiltro> parametros = new ParametrosConsulta<EmpresaFiltro>(new EmpresaFiltro { }, new PaginacionConsulta { });
            var idUsuario = 7;

            var filtro = parametros.Filtro;
            var q = Sql
                .FromTable<Empresa>()
                .Inner().JoinTable<GrupoEmpresa>().OnTuple(x => x.Item1.IdGrupoEmpresa == x.Item2.IdRegistro)
                .Inner().JoinTable<Cuenta>().On(x => x.Item1.IdCuenta == x.Item3.IdRegistro)
                //Filtrar sólo las empresas relacionadas con este usuario:
                .Inner().JoinTable<UsuarioEmpresa>().On(x => x.Item4.IdEmpresa == x.Item1.IdRegistro && x.Item4.IdUsuario == idUsuario)
                .Alias(x => new
                {
                    empresa = x.Item1,
                    grupo = x.Item2,
                    cuenta = x.Item3
                })
                .Select(from => Tonic.LinqEx.CloneSimpleSelector(from, x => x.empresa, x => new EmpresaDto
                {
                    NombreCuenta = x.cuenta.Nombre,
                    NombreGrupo = x.grupo.Nombre
                }).Invoke(from))
                .Where(x =>
                    SqlExpr.IfCond.Invoke(filtro.Destacado, x.empresa.Destacado)
                );

            var str = q.ToString();
         }
    }
}
