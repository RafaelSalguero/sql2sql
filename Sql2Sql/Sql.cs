using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Sql2Sql.Fluent.Data;
using Sql2Sql.Fluent;
using System.Collections;
using Sql2Sql.ExprRewrite;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Sql2Sql.Test")]

namespace Sql2Sql
{
    /// <summary>
    /// Funciones que se pueden traducir a SQL
    /// </summary>
    public static partial class Sql
    {
        /// <summary>
        /// Inica un query con un FROM dado el tipo de la tabla
        /// </summary>
        public static ISqlFirstJoinAble<TTable, TTable, object> From<TTable>() => From(new SqlTable<TTable>());

        /// <summary>
        /// Inica un query con un FROM dado el tipo de la tabla
        /// </summary>
        public static ISqlFirstJoinAble<TTable, TTable, object> From<TTable>(string table) => From(new SqlTable<TTable>(table));

        /// <summary>
        /// Inica una lista de WITH
        /// </summary>
        public static ISqlWithAble<IFromListItemTarget<T>> With<T>(ISqlSelect<T> select) =>
            new SqlWith<object, T, IFromListItemTarget<T>>(null, SqlWithType.Normal, x => select, null, (a, b) => b);

        /// <summary>
        /// Agrega un elemento a la lista de WITH
        /// </summary>
        public static ISqlWithUnionAble<object, T> WithRecursive<T>(ISqlSelect<T> select) =>
            new SqlWith<object, T, IFromListItemTarget<T>>(null, SqlWithType.Normal, x => select, null, (a, b) => b);


        /// <summary>
        /// Inicia un query con un FROM dado el destino del from
        /// </summary>
        [Obsolete("Use From<Table>()")]
        public static ISqlFirstJoinAble<T1, T1, object> From<T1>(SqlTable<T1> from) => From((IFromListItemTarget<T1>)from);


        /// <summary>
        /// Inicia un query con un FROM dado el destino del from
        /// </summary>
        /// <param name="from">Destino del FROM, puede ser un <see cref="SqlTable"/> o un subquery  </param>
        public static ISqlFirstJoinAble<T1, T1, object> From<T1>(IFromListItemTarget<T1> from) =>
            new SqlSelectBuilder<T1, T1, object>(new SelectClause<T1, T1, object>(new SqlFrom<T1>(from), SelectType.All, null, null, (x, win) => x, null, null, null, null));

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
        /// <param name="acum">Llamada a una función de acumulado</param>
        /// <param name="over">Referencia a un WINDOW</param>
        [AlwaysThrows]
        public static T Over<T>(T acum, ISqlWindow over) => throw new SqlFunctionException();

        /// <summary>
        /// Aplica un FILTER sobre el resultado de una función de acumulado
        /// </summary>
        /// <param name="acum">Llamada a una función de acumulado</param>
        [AlwaysThrows]
        public static T Filter<T>(T acum, bool cond) => throw new SqlFunctionException();

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
        /// </summary>
        [AlwaysThrows]
        public static bool Like(string text, string pattern) => throw new SqlFunctionException();

        /// <summary>
        /// Un record de postgres
        /// </summary>
        [AlwaysThrows]
        public static T Record<T>(T items) where T : IEnumerable => throw new SqlFunctionException();


    }
}
