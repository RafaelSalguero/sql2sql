using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeaSql.Fluent;
using KeaSql.Fluent.Data;

namespace KeaSql
{

    /// <summary>
    /// Un SELECT
    /// </summary>
    public interface ISqlSelectExpr<out TOut> : ISqlSelectExpr, ISqlSelect<TOut>
    {
    }

    public interface ISqlSelect : IFromListItemTarget { }

    /// <summary>
    /// Un SELECT
    /// </summary>
    public interface ISqlSelect<out T> : IFromListItemTarget<T>, ISqlSelect
    {
    }

    public interface ISqlWithSelect : ISqlSelect {
        WithSelectClause With { get; }
        ISqlSelect Query { get; }
    }

    /// <summary>
    /// Un WITH ... SELECT
    /// </summary>
    public interface ISqlWithSubquery<out T> : ISqlSelect<T> , ISqlWithSelect
    {
        WithSelectClause With { get; }
        ISqlSelect<T> Query { get; }
    }
}
