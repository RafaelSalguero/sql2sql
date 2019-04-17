using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql.Fluent.Data
{
    public interface IWithList { }
    public interface IWithList<T> : IWithList { }

    public interface ISqlWith
    {
        ISqlWith Left { get; }
        SqlWithType Type { get; }
        LambdaExpression Select { get; }
        LambdaExpression Recursive { get; }
        LambdaExpression Map { get; }
    }

    public interface ISqlWith<TRet> : ISqlWith
    {

    }

    public interface ISqlWithInSelect<TIn, TSelect>
    {
        SqlWithType Type { get; }
        ISqlWith<TIn> Left { get; }
        Expression<Func<TIn, IFromListItemTarget<TSelect>>> Select { get; }
        Expression<Func<TIn, IFromListItemTarget<TSelect>, IFromListItemTarget<TSelect>>> Recursive { get; }
    }


    public interface ISqlWithAble<TIn> : ISqlWith<TIn>
    {
    }

    public interface ISqlWithMapAble<TIn, TSelect> : ISqlWithInSelect<TIn, TSelect> { }
    public interface ISqlWithUnionAble<TIn, TSelect> : ISqlWithInSelect<TIn, TSelect> { }

    public enum SqlWithType
    {
        Normal,
        RecursiveUnion,
        RecursiveUnionAll,
    }

    public interface ISqlWithClause<TIn, TSelect, TRet> :
         ISqlWith<TRet>,
        ISqlWithAble<TRet>,
        ISqlWithMapAble<TIn, TSelect>,
        ISqlWithUnionAble<TIn, TSelect>
    { }

    public class SqlWith<TIn, TSelect, TRet> : ISqlWithClause<TIn, TSelect, TRet>
    {
        public SqlWith(ISqlWith<TIn> left, SqlWithType type, Expression<Func<TIn, IFromListItemTarget<TSelect>>> select, Expression<Func<TIn, IFromListItemTarget<TSelect>, IFromListItemTarget<TSelect>>> recursive, Expression<Func<TIn, IFromListItemTarget<TSelect>, TRet>> map)
        {
            Left = left;
            Type = type;
            Select = select;
            Recursive = recursive;
            Map = map;
        }

        public ISqlWith<TIn> Left { get; }
        public SqlWithType Type { get; }
        public Expression<Func<TIn, IFromListItemTarget<TSelect>>> Select { get; }
        public Expression<Func<TIn, IFromListItemTarget<TSelect>, IFromListItemTarget<TSelect>>> Recursive { get; }
        public Expression<Func<TIn, IFromListItemTarget<TSelect>, TRet>> Map { get; }

        LambdaExpression ISqlWith.Select => Select;
        LambdaExpression ISqlWith.Map => Map;
        ISqlWith ISqlWith.Left => Left;
        LambdaExpression ISqlWith.Recursive => Recursive;
    }


    /// <summary>
    /// Una cláusula de SELECT en función de un WITH
    /// </summary>
    /// <typeparam name="TWith"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    public class SqlWithFromList<TWith, TOut> : ISqlWithSubquery<TOut>
    {
        public SqlWithFromList(WithSelectClause with, ISqlSelect<TOut> query)
        {
            With = with;
            Query = query;
        }

        public WithSelectClause With { get; }
        public ISqlSelect<TOut> Query { get; }
        ISqSelect ISqlWithSelect.Query => Query;

        public override string ToString()
        {
            return this.ToSql(SqlText.ParamMode.Substitute).Sql;
        }
    }

    public class SqlWithBuilder<TIn, TSelect, TRet>
    {
        public SqlWithBuilder(SqlWith<TIn, TSelect, TRet> clause)
        {
            Clause = clause;
        }

        public SqlWith<TIn, TSelect, TRet> Clause { get; }
    }
}
