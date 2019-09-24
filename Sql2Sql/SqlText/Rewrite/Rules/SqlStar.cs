using Sql2Sql.ExprRewrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.SqlText.Rewrite.Rules
{
    /// <summary>
    /// SQL Star rules
    /// </summary>
    public static class SqlStar
    {
        //TODO: Find a way to represent varydic calls in the ExprRewriter
        public static RewriteRule[] starRules = new[]
         {
           RewriteRule.Create(
               "starAll",
               (RewriteTypes.C1 map) =>Sql.Star().Map(map),
               (RewriteTypes.C1 map) => Sql.Raw<RewriteTypes.C1>($"*,\r\n{SqlFunctions.ToSelectBodySql(map)}")
               ),

            RewriteRule.Create(
               "star1",
               (RewriteTypes.C1 map, RewriteTypes.C2 item1) =>Sql.Star(item1).Map(map),
               (RewriteTypes.C1 map, RewriteTypes.C2 item1) => Sql.Raw<RewriteTypes.C1>($"{SqlFunctions.ToSelectBodySql(item1)},\r\n{SqlFunctions.ToSelectBodySql(map)}")
               ),

            RewriteRule.Create(
                "star2",
                (RewriteTypes.C1 map, RewriteTypes.C2 item1, RewriteTypes.C3 item2) =>Sql.Star(item1, item2).Map(map),
                (RewriteTypes.C1 map, RewriteTypes.C2 item1, RewriteTypes.C3 item2) => Sql.Raw<RewriteTypes.C1>($"{SqlFunctions.ToSelectBodySql(item1)}, {SqlFunctions.ToSelectBodySql(item2)},\r\n{SqlFunctions.ToSelectBodySql(map)}")
                ),

            RewriteRule.Create(
                "star3",
                (RewriteTypes.C1 map, RewriteTypes.C2 item1, RewriteTypes.C3 item2,RewriteTypes.C4 item3   ) =>Sql.Star(item1, item2).Map(map),
                (RewriteTypes.C1 map, RewriteTypes.C2 item1, RewriteTypes.C3 item2, RewriteTypes.C4 item3  ) => Sql.Raw<RewriteTypes.C1>($"{SqlFunctions.ToSelectBodySql(item1)}, {SqlFunctions.ToSelectBodySql(item2)}, {SqlFunctions.ToSelectBodySql(item3)},\r\n{SqlFunctions.ToSelectBodySql(map)}")
                ),

            RewriteRule.Create(
                "star4",
                (RewriteTypes.C1 map, RewriteTypes.C2 item1, RewriteTypes.C3 item2, RewriteTypes.C4 item3, RewriteTypes.C5 item4   ) =>Sql.Star(item1, item2).Map(map),
                (RewriteTypes.C1 map, RewriteTypes.C2 item1, RewriteTypes.C3 item2, RewriteTypes.C4 item3, RewriteTypes.C5 item4  ) => Sql.Raw<RewriteTypes.C1>($"{SqlFunctions.ToSelectBodySql(item1)}, {SqlFunctions.ToSelectBodySql(item2)}, {SqlFunctions.ToSelectBodySql(item3)}, {SqlFunctions.ToSelectBodySql(item4)},\r\n{SqlFunctions.ToSelectBodySql(map)}")
                ),
       };
    }
}
