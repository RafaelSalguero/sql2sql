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

    public interface ISqSelect : IFromListItemTarget { }

    /// <summary>
    /// Un SELECT
    /// </summary>
    public interface ISqlSelect<out T> : IFromListItemTarget<T>, ISqSelect
    {
    }

    public interface ISqlWithSelect : ISqSelect {
        WithSelectClause With { get; }
        ISqSelect Query { get; }
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
