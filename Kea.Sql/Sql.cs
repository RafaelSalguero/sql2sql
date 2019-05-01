using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using KeaSql.Fluent.Data;
using KeaSql.Fluent;
using System.Collections;
using KeaSql.ExprRewrite;

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
        public static ISqlJoinAble<TTable> FromTable<TTable>() => From(new SqlTable<TTable>());

        /// <summary>
        /// Inica un query con un FROM dado el tipo de la tabla
        /// </summary>
        public static ISqlJoinAble<TTable> FromTable<TTable>(string table) => From(new SqlTable<TTable>(table));

        /// <summary>
        /// Inica una lista de WITH
        /// </summary>
        /// <param name="withObject">Un objeto donde cada propiedad es un query del WITH</param>
        public static ISqlWithAble<IFromListItemTarget<T>> With<T>(ISqlSelect<T> select) =>
            new SqlWith<object, T, IFromListItemTarget<T>>(null, SqlWithType.Normal, x => select, null, (a, b) => b);

        /// <summary>
        /// Agrega un elemento a la lista de WITH
        /// </summary>
        public static ISqlWithUnionAble<object, T> WithRecursive<T>(ISqlSelect<T> select) =>
            new SqlWith<object, T, IFromListItemTarget<T>>(null, SqlWithType.Normal, x => select, null, (a,b) => b);


        /// <summary>
        /// Inicia un query con un FROM dado el destino del from
        /// </summary>
        /// <param name="from">Destino del FROM, puede ser un <see cref="SqlTable"/> o un subquery  </param>
        public static ISqlJoinAble<T1> From<T1>(IFromListItemTarget<T1> from) =>
            new PreSelectPreWinBuilder<T1>(new PreSelectClause<T1, object>(new SqlFrom<T1>(from), SelectType.All, null, null));

        /// <summary>
        /// Representa el parametro del select que hace referencia a la lista de from.
        /// Note que esta función no lanza una excepción porque hay ocasiones donde se tiene que evaluar
        /// </summary>
        [AlwaysNull]
        internal static T FromParam<T>() => default(T);

        /// <summary>
        /// Sustituir la cadena especificada
        /// </summary>
        [AlwaysThrows]
        public static T Raw<T>(string sql) => throw new SqlFunctionException();

        /// <summary>
        /// Un SQL que se sustituirá tal cual, indicando que hace referencia a una fila del FROM-list
        /// </summary>
        [AlwaysThrows]
        internal static T RawRowRef<T>(string sql) => throw new SqlFunctionException();

        /// <summary>
        /// Un SQL que se sustituirá tal cual, indica que hace referencia a un nombre de una tabla, view, o a un elemento del WITH
        /// </summary>
        internal static IFromListItemTarget<T> RawTableRef<T>(string sql) => new SqlTableRefRaw<T>(sql);

        /// <summary>
        /// Un SQL que se sustituirá tal cual, indica que es un SELECT
        /// </summary>
        public static ISqlSelect<T> RawSubquery<T>(string sql) => new SqlSelectRaw<T>(sql);

        /// <summary>
        /// Aplica un OVER sobre el resultado de una función de acumulado y un WINDOW
        /// </summary>
        [AlwaysThrows]
        public static T Over<T>(T expr, ISqlWindow over) => throw new SqlFunctionException();

        /// <summary>
        /// Aplica un FILTER sobre el resultado de una función de acumulado
        /// </summary>
        [AlwaysThrows]
        public static T Filter<T>(T expr, bool cond) => throw new SqlFunctionException();

        /// <summary>
        /// Condición BETWEEN
        /// </summary>
        [AlwaysThrows]
        public static bool Between<T>(T expr, T min, T max) => throw new SqlFunctionException();

        /// <summary>
        /// Aplica un CAST(expr AS type)
        /// </summary>
        [AlwaysThrows]
        public static T Cast<T>(T expr, SqlType type) => throw new SqlFunctionException();

        /// <summary>
        /// Un LIKE
        [AlwaysThrows]
        public static bool Like(string text, string pattern) => throw new SqlFunctionException();

        /// <summary>
        /// Un record de postgres
        /// </summary>
        [AlwaysThrows]
        public static T Record<T>(T items) where T : IEnumerable => throw new SqlFunctionException();

        /// <summary>
        /// Aplica un item IN items
        /// </summary>
        [AlwaysThrows]
        public static bool In<T>(T item, IEnumerable<T> items) => throw new SqlFunctionException();
    }
}
