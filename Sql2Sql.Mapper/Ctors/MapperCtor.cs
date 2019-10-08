using Sql2Sql.Ctors;
using Sql2Sql.Mapper;
using Sql2Sql.Mapper.ComplexTypes;
using Sql2Sql.Mapper.ILCtors;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Ctors
{
    /// <summary>
    /// Se encarga de la construcción de los tipos mapeados
    /// </summary>
    static class MapperCtor
    {
        /// <summary>
        /// Las columnas del data reader y los paths del objeto
        /// </summary>
        public class ColPaths
        {
            public ColPaths(IReadOnlyDictionary<string, int> columns, ComplexTypePaths paths)
            {
                Columns = columns;
                Paths = paths;
            }

            /// <summary>
            /// Diccionario con los nombres de las columnas del data reader y sus indices
            /// </summary>
            public IReadOnlyDictionary<string, int> Columns { get; }

            /// <summary>
            /// Las rutas del objeto
            /// </summary>
            public ComplexTypePaths Paths { get; }
        }

        /// <summary>
        /// Indica la forma en la que se creará la instancia de un tipo que va a leer el mapper
        /// </summary>
        public enum InitMode
        {
            /// <summary>
            /// El tipo es simple, no se llamará a ningún constructor
            /// </summary>
            SimpleType,
            /// <summary>
            /// Se usará el constructor por default
            /// </summary>
            PublicDefaultConstructor,
            /// <summary>
            /// Se usará el único constructor público con argumentos
            /// </summary>
            SingleParametizedConstructor,
        }

        /// <summary>
        /// Lee el registro actual, en caso de que sea un valor singular, por ejemplo, un sólo numero o cadena
        /// </summary>
        /// <returns></returns>
        static object ReadCurrentSingular(IDataRecord reader, SingularMapping mapping)
        {
            var ret = ReadColumn(reader, mapping.ColumnId, mapping.Type);
            return cast.Cast(mapping.Type, ret);
        }

        /// <summary>
        /// Lee el valor actual. 
        /// Para crear una instancia del tipo se hace lo siguiente:
        /// - Primero se busca un constructor sin argumentos, en caso de que se encuentre, la inicialización del objeto es asignando sus propiedades
        /// - Si no, se busca un constructor único con argumentos, si hay más de uno lanza una excepción
        /// </summary>
        public static object ReadCurrent(IDataRecord reader, ColPaths cp, ColumnMatchMode mode, Type type)
        {
            var mapping = Mapper.Ctors.MappingLogic.CreateMapping(type, reader);
            return Init(reader, mapping);
        }

        static object Init(IDataRecord reader, ValueMapping mapping)
        {
            switch (mapping)
            {
                case Mapper.ILCtors.SingularMapping sing:
                    return ReadCurrentSingular(reader, sing);
                case Mapper.ILCtors.CtorMapping ctor:
                    return InitObject(reader, ctor);
                case Mapper.ILCtors.NullMapping n:
                    throw new ArgumentException($"Could not found any mapping for the type '{n.Type}'");
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Quita las columnas que ya se usaron
        /// </summary>
        static ColPaths RemoveUsedColumns(ColPaths cp, IEnumerable<string> usedColumns)
        {
            var cols = cp.Columns.Where(x => !usedColumns.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            var pathCols = cp.Paths.Paths.Where(x => !usedColumns.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);

            var paths = new ComplexTypePaths(pathCols, cp.Paths.Types);
            return new ColPaths(cols, paths);
        }

        /// <summary>
        /// Inicializa un objeto dado su constructor, escribe las propiedades de las columnas que no estuvieron incluídas en los parámetros del constructor
        /// </summary>
        static object InitObject(IDataRecord reader, CtorMapping mapping)
        {
            var pars = mapping.ConstructorColumnMapping.Select(x => Init(reader, x)).ToArray();
            var instance = mapping.Constructor.Invoke(pars);

            var props = mapping.PropertyMapping.Select(x => new
            {
                prop = x.Key,
                value = Init(reader, x.Value)
            }).ToList();

            foreach (var p in props)
            {
                p.prop.SetValue(instance, p.value);
            }
             
            return instance;
        }

        /// <summary>
        /// Devuelve las inicializaciones de cierta colección de propiedades.
        /// Devuelve un elemento por cada propiedad. Si no se encuentran columnas para cierta propiedad ese elemento será null
        /// </summary>
        /// <returns></returns>
        static (IReadOnlyList<PropertyInit> inits, IReadOnlyList<string> usedColumns) GetPropertyInits(IEnumerable<PropertyInfo> props, IDataRecord reader, ColPaths cp, ColumnMatchMode mode)
        {
            //Columnas usadas por el inicializador del constructor:
            var usedColumns = new List<string>();
            var ret = new List<PropertyInit>();
            foreach (var prop in props)
            {

                //Si el prop encaja directamente con un path, buscar la columna que corresponde y asignarlo, esto sólo funcionará para los tipos simples,
                //ya que los tipos complejos pueden tener más de una columna
                var singlePath = GetSinglePathItemFromProp(prop.Name, cp.Paths);
                if (singlePath != null)
                {
                    //Indice de la columna
                    if (!cp.Columns.TryGetValue(singlePath?.column, out int colIndex))
                    {
                        ret.Add(null);
                    }
                    else
                    {
                        usedColumns.Add(singlePath.Value.column);
                        ret.Add(new PropertyInit(ReadColumn(reader, colIndex, singlePath.Value.path.PropType)));
                    }

                    continue;
                }
                else
                {
                    //Ligar las subrutas:
                    var subCp = GetSubpaths(cp, prop.Name);
                    usedColumns.AddRange(subCp.Columns.Select(x => x.Key));

                    if (!subCp.Columns.Any())
                    {
                        //No hay columnas para esta propiedad
                        ret.Add(null);
                    }
                    else
                    {
                        //Resolver la propiedad con los subpaths:
                        ret.Add(new PropertyInit(ReadCurrent(reader, subCp, mode, prop.PropertyType)));
                    }
                }
            }

            return (ret, usedColumns);
        }

    

        /// <summary>
        /// Obtiene la propiedad ligada a un parámetro de un constructor, para esto la busca por nombre sin importar el case
        /// </summary>
        static PropertyInfo GetPropertyByParam(IEnumerable<PropertyInfo> props, ParameterInfo par)
        {
            var ps = props.Where(x => x.Name.ToLower() == par.Name.ToLower()).ToList();
            if (!ps.Any())
                throw new ArgumentException($"No se encontró ninguna propiedad ligada al parámetro '{par.Name}'");

            if (ps.Count > 1)
                throw new ArgumentException($"Existe más de una propiedad que encaja con el nombre del parámetro '{par.Name}'");

            return ps.Single();
        }

        static T? AsNullable<T>(T x) where T : struct => new T?(x);

        /// <summary>
        /// Obtiene la columna y el access path que le corresponde a una propiedad del objeto, en caso de que la propiedad encaje con una ruta que sea hijo directo del tipo,
        /// esto significa que las propiedades de los tipos complejos van a devolver null
        /// </summary>
        static (string column, AccessPathItem path)? GetSinglePathItemFromProp(string prop, ComplexTypePaths paths)
        {
            var path = paths.Paths
                //Sólo hijos directos:
                .Where(x => x.Value.Count == 1)
                .Select(x => (col: x.Key, path: x.Value.Single()))
                .Where(x => x.path.Name == prop)
                .Select(AsNullable)
                .SingleOrDefault();

            return path;
        }

        /// <summary>
        /// Obtiene el subconjunto de columnas y paths que corresponden a una propiedad del objeto padre
        /// </summary>
        static ColPaths GetSubpaths(ColPaths parent, string prop)
        {
            var subpaths = parent.Paths.Paths
                .Where(x => x.Value.First().Name == prop && x.Value.Count >= 2)
                .ToList()
                .ToDictionary(
                x => x.Key,
                //Note que nos tenemos que saltar el primer elemento de la ruta, ya que la raíz de la ruta ahora será el segundo elemento:
                x => x.Value.Skip(1).ToList())
                ;

            var subcols = subpaths.ToDictionary(x => x.Key, x =>
            {
                if (!parent.Columns.TryGetValue(x.Key, out int ret))
                    throw new ArgumentException($"No se encontró la columna '{x.Key}'");
                return ret;
            });

            return new ColPaths(subcols, new ComplexTypePaths(subpaths, parent.Paths.Types));
        }

        ///<summary>
        /// Obtiene el modo de inicialización y el constructor relacionado de cierto tipo
        ///</summary>
        static (InitMode mode, ConstructorInfo cons) GetInitMode(Type type)
        {
            if (PathAccessor.IsSimpleType(type))
                return (InitMode.SimpleType, null);

            if (!type.IsClass)
                throw new ArgumentException($"El tipo '{type}' debe de ser una clase");

            //Constructor por default:
            var cons = type.GetConstructors();
            if (cons.Length == 0)
                throw new ArgumentException($"El tipo '{type}' no tiene constructores públicos");

            var defaultCons = cons.Where(x => x.GetParameters().Length == 0).FirstOrDefault();
            if (defaultCons != null)
                return (InitMode.PublicDefaultConstructor, defaultCons);

            //Constructor único parametrizado:
            if (cons.Length > 1)
                throw new ArgumentException($"El tipo '{type}' tiene más de un constructor parametrizado");

            var paramCons = cons.Single();
            return (InitMode.SingleParametizedConstructor, paramCons);
        }

        static ExprCast cast = new ExprCast();

        /// <summary>
        /// Determina si cierta prueba la pasa el tipo, o el tipo interno de un Nullable
        /// </summary>
        static bool IsTypeOrNullable(Type testedType, Func<Type, bool> test, out Type nonNullType)
        {
            if (test(testedType))
            {
                nonNullType = testedType;
                return true;
            }
            if (testedType.IsGenericType && testedType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var t = testedType.GetGenericArguments()[0];
                nonNullType = t;
                return test(t);
            }
            nonNullType = null;
            return false;
        }

        static DateTimeOffset ToDateTimeOffset(DateTime date)
        {
            if (date.Kind == DateTimeKind.Unspecified)
                throw new ArgumentException("No se puede convertir un DateTime con Kind = 'Unspecified' a DateTimeOffset");

            DateTimeOffset ret = date;
            return ret;
        }

        /// <summary>
        /// Lee el valor de una columna de un IDataRecord
        /// </summary>
        /// <param name="reader">Fila a leer</param>
        /// <param name="column">Índice de la columna</param>
        /// <param name="colType">Tipo de la columna</param>
        /// <returns>El valor de la columna</returns>
        static object ReadColumn(IDataRecord reader, int column, Type colType)
        {
            if (reader.IsDBNull(column))
            {
                return null;
            }

            var value = reader.GetValue(column);

            if (IsTypeOrNullable(colType, x => x == typeof(DateTimeOffset), out var _) && value is DateTime date)
            {
                return ToDateTimeOffset(date);
            }
            if (IsTypeOrNullable(colType, x => x.IsEnum, out var enumType))
            {
                //Si es enum:
                return Enum.ToObject(enumType, value);
            }
            return value;
        }

        /// <summary>
        /// Lee el valor de una columna de <paramref name="reader"/>, considerando los tipos de la clase ligada a este <see cref="DbMapper{T}"/>
        /// </summary>
        static object ReadClassColumn(IDataRecord reader, ColPaths cp, string column)
        {
            var colType = cp.Paths.Paths[column].Last().PropType;
            var colIndex = cp.Columns[column];
            return ReadColumn(reader, colIndex, colType);
        }


    }
}
