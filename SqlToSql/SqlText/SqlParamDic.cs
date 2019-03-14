using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SqlToSql.ExprTree;

namespace SqlToSql.SqlText
{
    public class SqlParamItem
    {
        public SqlParamItem(object target, IReadOnlyList<MemberInfo> path, string paramName, int paramIndex)
        {
            Target = target;
            Path = path;
            ParamName = paramName;
            ParamIndex = paramIndex;
        }

        /// <summary>
        /// Objeto de la captura del parametro
        /// </summary>
        public object Target { get; }

        /// <summary>
        /// Miembros que se tienen que acceder del target para obtener el nombre del parámetro
        /// </summary>
        public IReadOnlyList<MemberInfo> Path { get; }

        /// <summary>
        /// Nombre del parámetro
        /// </summary>
        public string ParamName { get; }

        /// <summary>
        /// Indice del parámetro
        /// </summary>
        public int ParamIndex { get; }

        /// <summary>
        /// Obtiene 
        /// </summary>
        /// <returns></returns>
        public object GetValue()
        {
            var val = Target;
            foreach (var p in Path)
            {
                if (p is FieldInfo field)
                {
                    val = field.GetValue(val);
                }
                else if (p is PropertyInfo prop)
                {
                    val = prop.GetValue(val);
                }
            }
            return val;
        }
    }

    /// <summary>
    /// Diccionario de parámetros
    /// </summary>
    public class SqlParamDic
    {
        public List<SqlParamItem> Items { get; } = new List<SqlParamItem>();

        string GetNewName(string hint, int? count)
        {
            var name = $"{hint}{count}";
            if (Items.Any(x => x.ParamName == name))
                return GetNewName(hint, (count ?? 0) + 1);
            else
                return name;
        }


        /// <summary>
        /// Agrega un parametro y devuelve el número del mismo, si ya existe devuelve el numero del parámetro existente
        /// </summary>
        public SqlParamItem AddParam(object target, IReadOnlyList<MemberInfo> path)
        {

            var it = Items.FirstOrDefault(x =>
                x.Target == target &&
                x.Path.SequenceEqual(path, CompareExpr.CompareMemberInfo)
            );

            if (it == null)
            {
                var name = GetNewName(path.Last().Name, null);
                it = new SqlParamItem(target, path, name, Items.Count);
                Items.Add(it);
            }
            return it;
        }
    }
}
