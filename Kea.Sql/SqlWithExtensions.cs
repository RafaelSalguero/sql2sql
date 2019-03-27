using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using KeaSql.Fluent;
using KeaSql.Fluent.Data;

namespace KeaSql
{
    /// <summary>
    /// Extensiones para construir lista de WITH
    /// </summary>
    public static class SqlWithExtensions
    {
        /// <summary>
        /// Agrega un elemento a la lista de WITH
        /// </summary>
        public static ISqlWithMapAble<TIn, TSelect> With<TIn, TSelect>(this ISqlWithAble<TIn> input, Expression<Func<TIn, IFromListItemTarget<TSelect>>> select) =>
            new SqlWith<TIn, TSelect, object>(input, SqlWithType.Normal, select, null, null);

        /// <summary>
        /// Agrega un elemento a la lista de WITH
        /// </summary>
        public static ISqlWithUnionAble<TIn, TSelect> WithRecursive<TIn, TSelect>(this ISqlWithAble<TIn> input, Expression<Func<TIn, IFromListItemTarget<TSelect>>> select) =>
            new SqlWith<TIn, TSelect, object>(input, SqlWithType.Normal, select, null, null);

        /// <summary>
        /// Agrega un UNION despues del SELECT del WITH RECURSIVE
        /// </summary>
        public static ISqlWithMapAble<TIn, TSelect> Union<TIn, TSelect>(this ISqlWithUnionAble<TIn, TSelect> input, Expression<Func<TIn, IFromListItemTarget<TSelect>, IFromListItemTarget<TSelect>>> recursive) =>
            new SqlWith<TIn, TSelect, object>(input.Left, SqlWithType.RecursiveUnion, input.Select, recursive, null);

        /// <summary>
        /// Agrega un UNION ALL despues del SELECT del WITH RECURSIVE
        /// </summary>
        public static ISqlWithMapAble<TIn, TSelect> UnionAll<TIn, TSelect>(this ISqlWithUnionAble<TIn, TSelect> input, Expression<Func<TIn, IFromListItemTarget<TSelect>, IFromListItemTarget<TSelect>>> recursive) =>
            new SqlWith<TIn, TSelect, object>(input.Left, SqlWithType.RecursiveUnionAll, input.Select, recursive, null);

        /// <summary>
        /// Mapea el elemento actual y el elemento anterior de una lista de WITH
        /// </summary>
        public static ISqlWithAble<TRet> Map<TIn, TSelect, TRet>(this ISqlWithMapAble<TIn, TSelect> input, Expression<Func<TIn, IFromListItemTarget<TSelect>, TRet>> map) =>
            new SqlWith<TIn, TSelect, TRet>(input.Left,input.Type, input.Select, input.Recursive, map);
    }
}
