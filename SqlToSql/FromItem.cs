using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
namespace SqlToSql
{
    public static class SqlToSql
    {

        public static SqlFromAble<T> With<T>(T queries) => throw new NotImplementedException();
        public static SqlJoinAble<TRet, object> From<TRet>(IEnumerable<TRet> table) => throw new NotImplementedException();
        public static SqlJoinAble<TRet, TWith> From<TRet, TWith>(this SqlFromAble<TWith> with, Func<TWith, IEnumerable<TRet>> table) => throw new NotImplementedException();

        public static SqlJoinAble<TRet, TWith> LeftJoin<T1, T2, TWith, TRet>(this SqlJoinAble<T1, TWith> left, Func<TWith, IEnumerable<T2>> right, Func<T1, T2, TRet> selector, Func<TRet, bool> on) => throw new NotImplementedException();
        public static SqlJoinAble<TRet, TWith> Join<T1, T2, TWith, TRet>(this SqlJoinAble<T1, TWith> left, Func<TWith, IEnumerable<T2>> right, Func<T1, T2, TRet> selector, Func<TRet, bool> on) => throw new NotImplementedException();
        public static SqlJoinAble<TRet, TWith> CrossJoin<T1, T2, TWith, TRet>(this SqlJoinAble<T1, TWith> left, Func<TWith, IEnumerable<T2>> right, Func<T1,T2,TRet> selector) => throw new NotImplementedException();

        public static SqlSelectAble<T> DistinctOn<T, TOn>(this SqlDistinctOnAble<T> query, Func<T, TOn> expression) => throw new NotImplementedException();
        public static SqlSelectAble<T> Distinct<T>(this SqlDistinctOnAble<T> query) => throw new NotImplementedException();
        public static SqlWherable<TResult> Select<T, TResult>(this SqlSelectAble<T> query, Func<T, TResult> selector) => throw new NotImplementedException();

        public static SqlGroupByAble<T> Where<T>(this SqlWherable<T> query, Func<T, bool> condition) => throw new NotImplementedException();
        public static SqlHavingAble<T> GroupBy<T>(this SqlGroupByAble<T> query, Func<T, object> expr) => throw new NotImplementedException();
        public static SqlWindowAble<T> Having<T>(this SqlHavingAble<T> query, Func<T, bool> condition) => throw new NotImplementedException();

        public static SqlUnionIntersectExceptAble<TRet> Window<T, TRet>(this SqlWindowAble<T> query) => throw new NotImplementedException();

        public static SqlOrderByAble<T> Union<T>(this SqlUnionAble<T> query, Func<SqlSelect<T>> other) => throw new NotImplementedException();
        public static SqlUnionIntersectExceptAllAble<T> Union<T>(this SqlUnionAble<T> query) => throw new NotImplementedException();
        public static SqlOrderByAble<T> Intersect<T>(this SqlIntersectAble<T> query, Func<SqlSelect<T>> other) => throw new NotImplementedException();
        public static SqlUnionIntersectExceptAllAble<T> Intersect<T>(this SqlIntersectAble<T> query) => throw new NotImplementedException();
        public static SqlOrderByAble<T> Except<T>(this SqlExceptAble<T> query, Func<SqlSelect<T>> other) => throw new NotImplementedException();
        public static SqlUnionIntersectExceptAllAble<T> Except<T>(this SqlExceptAble<T> query) => throw new NotImplementedException();
        public static SqlOrderByAble<T> All<T>(this SqlUnionIntersectExceptAllAble<T> query, Func<SqlSelect<T>> other) => throw new NotImplementedException();

        public static SqlOrderByAscDescAble<T> OrderBy<T, TExpr>(this SqlOrderByAble<T> query, Func<T, TExpr> expr) => throw new NotImplementedException();

        public static SqlOrderByNullsAble<T> Ascending<T>(this SqlOrderByAscDescAble<T> query) => throw new NotImplementedException();
        public static SqlOrderByNullsAble<T> Descending<T>(this SqlOrderByAscDescAble<T> query) => throw new NotImplementedException();

        public static SqlOrderByThenByAble<T> NullsFirst<T>(this SqlOrderByNullsAble<T> query) => throw new NotImplementedException();
        public static SqlOrderByThenByAble<T> NullsLast<T>(this SqlOrderByNullsAble<T> query) => throw new NotImplementedException();

        public static SqlOrderByAscDescAble<T> ThenBy<T, TExpr>(this SqlOrderByThenByAble<T> query, Func<T, TExpr> expr) => throw new NotImplementedException();
        public static SqlOffsetAble<T> Limit<T>(this SqlLimitAble<T> query, int count) => throw new NotImplementedException();
        public static SqlForUpdateShareAble<T> Offset<T>(this SqlOffsetAble<T> query, int count) => throw new NotImplementedException();

        public static SqlForUpdateShareNoWaitAble<T> ForUpdate<T>(this SqlForUpdateShareAble<T> query, params object[] tableNames) => throw new NotImplementedException();
        public static SqlForUpdateShareNoWaitAble<T> ForShare<T>(this SqlForUpdateShareAble<T> query, params object[] tableNames) => throw new NotImplementedException();
        public static SqlSelectAble<T> NoWait<T>(this SqlForUpdateShareNoWaitAble<T> query) => throw new NotImplementedException();
        

        private static MethodInfo select = typeof(SqlToSql).GetMethod("Select");
        private static MethodInfo fromSimple = typeof(SqlToSql).GetMethods().Where(x => x.Name == "From" && x.GetParameters().Length == 1).First();
        private static MethodInfo fromWith = typeof(SqlToSql).GetMethods().Where(x => x.Name == "From" && x.GetParameters().Length == 2).First();

       



    }


    public interface SqlSelect<T> { } 

    public interface SqlForUpdateShareNoWaitAble<T> : SqlSelect <T>{ }

    public interface SqlForUpdateShareAble<T> : SqlSelect<T> { }
    public interface SqlFetchAble<T> : SqlForUpdateShareAble<T> { }
    public interface SqlOffsetAble<T> : SqlFetchAble<T> { }
    public interface SqlLimitAble<T> : SqlOffsetAble<T> { }

    public interface SqlOrderByThenByAble<T> : SqlLimitAble<T> { }
    public interface SqlOrderByNullsAble<T> : SqlOrderByThenByAble<T> { }
    public interface SqlOrderByAscDescAble<T> : SqlOrderByNullsAble<T> { }

    public interface SqlOrderByAble<T> : SqlLimitAble<T> { }

    public interface SqlUnionIntersectExceptQueryAble<T> { };
    public interface SqlUnionIntersectExceptAllAble<T> { };

    public interface SqlUnionAble<T> : SqlOrderByAble<T> { }
    public interface SqlIntersectAble<T> : SqlOrderByAble<T> { }
    public interface SqlExceptAble<T> : SqlOrderByAble<T> { }
    
    public interface SqlUnionIntersectExceptAble<T> : SqlUnionAble<T>, SqlIntersectAble<T>, SqlExceptAble<T> { }

    public interface SqlWindowAble<T> : SqlUnionIntersectExceptAble<T> { }
    public interface SqlHavingAble<T> : SqlWindowAble<T> { }
    public interface SqlGroupByAble<T> : SqlHavingAble<T> { }
    public interface SqlWherable<T> : SqlGroupByAble<T> { }


    public interface SqlSelectAble<T> { }

    public interface SqlDistinctAble<T> : SqlSelectAble<T> { }
    public interface SqlDistinctOnAble<T> : SqlSelectAble<T> { }
    public interface SqlDistinctDistinctOnAble<T> : SqlDistinctAble<T>, SqlDistinctOnAble<T> { }


    public interface SqlJoinAble<T, TWith> : SqlDistinctDistinctOnAble<T> { }
    public interface SqlFromAble<TWith> { }

}
