using Sql2Sql.ExprRewrite;
using Sql2Sql.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql
{
    

    public static partial class Sql
    {
        /// <summary>
        /// A SQL Star (*) operator for all the items in the from-list
        /// </summary>
        [AlwaysThrows]
        public static ISqlStar Star() => throw new SqlFunctionException();

        /// <summary>
        /// A SQL Star (*) operator for a given item in the from-list
        /// </summary>
        [AlwaysThrows]
        public static ISqlStar Star<T1>(T1 item1) => throw new SqlFunctionException();

        /// <summary>
        /// A SQL Star (*) operator for a given items in the from-list
        /// </summary>
        [AlwaysThrows]
        public static ISqlStar Star<T1, T2>(T1 item1, T2 item2) => throw new SqlFunctionException();

        /// <summary>
        /// A SQL Star (*) operator for a given items in the from-list
        /// </summary>
        [AlwaysThrows]
        public static ISqlStar Star<T1, T2, T3>(T1 item1, T2 item2, T3 item3) => throw new SqlFunctionException();

        /// <summary>
        /// A SQL Star (*) operator for a given items in the from-list
        /// </summary>
        [AlwaysThrows]
        public static ISqlStar Star<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4) => throw new SqlFunctionException();

        /// <summary>
        /// Add extra columns to an SQL Star operator
        /// </summary>
        /// <param name="map">Extra columns to add</param>
        [AlwaysThrows]
        public static T Map<T>(this ISqlStar star, T map) => throw new SqlFunctionException();
    }
}
