using Kea.Mapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Kea
{
    /// <summary>
    /// Se encarga de la construcción de los tipos mapeados
    /// </summary>
    public static class MapperCtor
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
        static object ReadCurrentSingular(IDataRecord reader, ColPaths cp, Type colType)
        {
            if (cp.Columns.Count != 1)
                throw new ArgumentException("El query devolvió más de 1 columna, y el tipo de retorno del query es uno singular");

            var ret = ReadColumn(reader, 0, colType);
            return cast.Cast(colType, ret);
        }

        /// <summary>
        /// Lee el valor actual. 
        /// 
        /// Para crear una instancia del tipo se hace lo siguiente:

        /// - Primero se busca un constructor sin argumentos, en caso de que se encuentre, la inicialización del objeto es asignando sus propiedades
        /// - Si no, se busca un constructor único con argumentos, si hay más de uno lanza una excepción
        /// </summary>
        public static object ReadCurrent(IDataRecord reader, ColPaths cp, ColumnMatchMode mode, Type type)
        {
            var initMode = MapperCtor.GetInitMode(type);

            switch (initMode.mode)
            {
                case InitMode.SimpleType:
                    return ReadCurrentSingular(reader, cp, type);
                case InitMode.PublicDefaultConstructor:
                    {
                        var ret = initMode.cons.Invoke(new object[0]);
                        PopulateInstance(reader, ret, cp, mode);
                        return ret;
                    }
                case InitMode.SingleParametizedConstructor:
                    return ConstructInstance(reader, initMode.cons, cp, mode);
            }
            throw new ApplicationException("initMode");
        }

        /// <summary>
        /// Lee el registro actual del DbDataReader llenando el objeto 'dest'
        /// </summary>
        public static void PopulateInstance(IDataRecord reader, object dest, ColPaths cp, ColumnMatchMode mode)
        {
            var type = dest.GetType();
            foreach (var colIx in cp.Columns)
            {
                var col = colIx.Key;

                if (!cp.Paths.Paths.TryGetValue(col, out var path))
                {
                    switch (mode)
                    {
                        case ColumnMatchMode.Source:
                            throw new ArgumentException($"No se encontró la columna '{col}' en el tipo {type}");
                        case ColumnMatchMode.Ignore:
                            continue;
                        default:
                            throw new ArgumentException(nameof(mode));
                    }
                }

                object value;
                try
                {
                    value = ReadClassColumn(reader, cp, col);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"No se pudo leer el valor de la columna '{col}'", ex);
                }

                try
                {
                    PathAccessor.SetPathValue(dest, path, cast, value);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"No se pudo asignar el valor '{value}' de tipo '{value.GetType()}' a la propiedad '{col}' del tipo '{type}'", ex);
                }
            }
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

        static Nullable<T> AsNullable<T>(T x) where T : struct => new Nullable<T>(x);

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
                x => (IReadOnlyList<AccessPathItem>)x.Value.Skip(1).ToList())
                ;

            var subcols = subpaths.ToDictionary(x => x.Key, x => parent.Columns[x.Key]);

            return new ColPaths(subcols, new ComplexTypePaths(subpaths, parent.Paths.Types));
        }

        /// <summary>
        /// Lee el registro actual de DbDataReader creando una nueva instancia de T con el constructor especificado
        /// </summary>
        static object ConstructInstance(IDataRecord reader, ConstructorInfo cons, ColPaths cp, ColumnMatchMode mode)
        {
            //Se tiene que asignar un valor a cada uno de los parametros del constructor:
            var pars = cons.GetParameters();
            //Valores de los parámetros:
            var parValues = new List<object>();
            var type = cons.DeclaringType;
            var props = type.GetProperties();
            foreach (var par in pars)
            {
                //Ligar el parámetro a una propiedad, y luego a un path
                var prop = GetPropertyByParam(props, par);

                {
                    //Si el prop encaja directamente con un path, buscar la columna que corresponde y asignarlo, esto sólo funcionará para los tipos simples,
                    //ya que los tipos complejos pueden tener más de una columna
                    var singlePath = GetSinglePathItemFromProp(prop.Name, cp.Paths);
                    if (singlePath != null)
                    {
                        //Indice de la columna
                        if (!cp.Columns.TryGetValue(singlePath?.column, out int colIndex))
                        {
                            throw new ArgumentException($"No se encontró la columna para el parámetro '{par.Name}' del constructor del tipo '{type.Name}'");
                        }
                        parValues.Add(ReadColumn(reader, colIndex, singlePath.Value.path.PropType));
                        continue;
                    }
                }


                {
                    //Ligar las subrutas:
                    var subCp = GetSubpaths(cp, prop.Name);
                    if (!subCp.Columns.Any())
                        throw new ArgumentException($"No se encontró ninguna columna que encaje con el parámetro '{par.Name}' del constructor del tipo '{type.Name}'");

                    //Resolver la propiedad con los subpaths:
                    parValues.Add(ReadCurrent(reader, subCp, mode, prop.PropertyType));
                    continue;
                }
            }

            //Lamar al constructor con los parametros:
            var ret = cons.Invoke(parValues.ToArray());
            return ret;
        }

        /// <summary>
        /// Obtiene el modo de inicialización y el constructor relacionado de cierto tipo
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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
