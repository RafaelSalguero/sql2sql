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
    public interface ISqlSelectExpr<TOut> : ISqlSelectExpr, ISqlSelect<TOut>
    {
    }

    public interface ISqSelect : IFromListItemTarget { }

    /// <summary>
    /// Un SELECT
    /// </summary>
    public interface ISqlSelect<T> : IFromListItemTarget<T>, ISqSelect
    {
    }

    public interface ISqlWithSelect : ISqSelect {
        WithSelectClause With { get; }
        ISqSelect Query { get; }
    }

    /// <summary>
    /// Un WITH ... SELECT
    /// </summary>
    public interface ISqlWithSubquery<T> : ISqlSelect<T> , ISqlWithSelect
    {
        WithSelectClause With { get; }
        ISqlSelect<T> Query { get; }
    }
}
