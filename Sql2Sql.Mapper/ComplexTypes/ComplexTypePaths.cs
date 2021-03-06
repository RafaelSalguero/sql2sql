﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Sql2Sql.Mapper.ComplexTypes
{
    /// <summary>
    /// Lista las columnas de una entidad, incluyendo recursivamente las propiedades de los tipos complejos
    /// </summary>
    public class ComplexTypePaths
    {
        public ComplexTypePaths(Dictionary<string, List<AccessPathItem>> paths, List<Type> types)
        {
            Paths = paths;
            Types = types;
        }

        /// <summary>
        /// Cada una de las columnas y su ruta de acceso.
        /// </summary>
        public Dictionary<string, List<AccessPathItem>> Paths { get; }

        /// <summary>
        /// Todos los tipos de los que se extrajeron propiedades, el primer elemento es siempre el tipo de la entidad y los demás son los tipos complejos analizados
        /// </summary>
        public List<Type> Types { get; }
    }
}
