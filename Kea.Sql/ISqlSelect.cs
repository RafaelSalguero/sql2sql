using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeaSql.Fluent;

namespace KeaSql
{
    /// <summary>
    /// Un SELECT
    /// </summary>
    public interface ISqlSelect<TOut> : ISqlSelect, IFromListItemTarget<TOut>
    {
    }

    /// <summary>
    /// Un SELECT
    /// </summary>
    public interface ISqlSubQuery<T> : IFromListItemTarget<T> { }
}
