using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using LinqKit;
using KeaSql.Fluent.Data;
using KeaSql.Fluent;

namespace KeaSql
{
    /// <summary>
    /// Funciones que se pueden traducir a SQL
    /// </summary>
    public static partial class Sql
    {
        /// <summary>
        /// Inica un query con un FROM dado el tipo de la tabla
        /// </summary>
        public static ISqlJoinAble<TTable> From<TTable>() => From(new SqlTable<TTable>());

        /// <summary>
        /// Inica una lista de WITH
        /// </summary>
        /// <param name="withObject">Un objeto donde cada propiedad es un query del WITH</param>
        public static ISqlWithAble<ISqlSelect<T>> With<T>(ISqlSelect<T> select) => new SqlWith<object, T, ISqlSelect<T>>();

        /// <summary>
        /// Inicia un query con un FROM dado el destino del from
        /// </summary>
        /// <param name="from">Destino del FROM, puede ser un <see cref="SqlTable"/> o un subquery  </param>
        public static ISqlJoinAble<T1> From<T1>(IFromListItemTarget<T1> from) =>
            new PreSelectPreWinBuilder<T1>(new PreSelectClause<T1, object>(new SqlFrom<T1>(from), SelectType.All, null, null));

        /// <summary>
        /// Sustituir la cadena especificada
        /// </summary>
        public static object Raw(string sql) => throw new SqlFunctionException();
        /// <summary>
        /// Sustituir la cadena especificada
        /// </summary>
        public static T Raw<T>(string sql) => throw new SqlFunctionException();

        /// <summary>
        /// Un SQL que se sustituirá tal cual, indicando que este es el nombre de una tabla, view, o que hace referencia a un elemento del FROM-list
        /// </summary>
        public static T RawTableRef<T>(string sql) => throw new SqlFunctionException();

        /// <summary>
        /// Aplica un OVER sobre el resultado de una función de acumulado y un WINDOW
        /// </summary>
        public static T Over<T>(T expr, ISqlWindow over) => throw new SqlFunctionException();

        /// <summary>
        /// Aplica un CAST(expr AS type)
        /// </summary>
        public static T Cast<T>(T expr, SqlType type) => throw new SqlFunctionException();

    }
}
