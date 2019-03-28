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
    public interface ISqlSelect<TOut> : ISqlSelect, ISqlSubQuery<TOut>
    {
    }

    /// <summary>
    /// Un SELECT
    /// </summary>
    public interface ISqlSubQuery<T> : IFromListItemTarget<T> { }

    /// <summary>
    /// Un WITH ... SELECT
    /// </summary>
    public interface ISqlWithSubQuery<T> : ISqlSubQuery<T>
    {
        WithSelectClause With { get; }
        ISqlSubQuery<T> Query { get; }
    }
}
