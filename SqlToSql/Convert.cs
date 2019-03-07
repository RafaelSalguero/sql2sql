using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SqlToSql
{
    public static class Convert
    {
        public static string GetSqlText<TDb, TResult>(TDb db, Expression<Func<SqlSelect<TResult>>> query)
        {
            throw new NotImplementedException();
        }

      
    }
}
