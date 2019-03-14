using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeaSql
{
    /// <summary>
    /// Indica el orden del ORDER BY
    /// </summary>
    public enum OrderByOrder
    {
        Asc,
        Desc
    }

    /// <summary>
    /// Indica el orden de los nulos en el ORDER BY
    /// </summary>
    public enum OrderByNulls
    {
        NullsFirst,
        NullsLast
    }

}
