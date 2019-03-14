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
        public SqlParamItem(object target, FieldInfo field, string paramName, int paramIndex)
        {
            Target = target;
            Field = field;
            ParamName = paramName;
            ParamIndex = paramIndex;
        }

        /// <summary>
        /// Objeto de la captura del parametro
        /// </summary>
        public object Target { get; }

        /// <summary>
        /// Campo relacionado con el target, que almacena el valor del parámetro
        /// </summary>
        public FieldInfo Field { get; }

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
            return Field.GetValue(Target);
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
        public SqlParamItem AddParam(object target, MemberInfo field)
        {
            if (field is FieldInfo f)
            {
                var it = Items.FirstOrDefault(x => x.Target == target && CompareExpr.CompareMemberInfo(x.Field, field));
                if (it == null)
                {
                    var name = GetNewName(field.Name, null);
                    it = new SqlParamItem(target, f, name, Items.Count);
                    Items.Add(it);
                }
                return it;
            }
            else
                throw new ArgumentException("Field debe de ser un FieldInfo");
        }
    }
}
