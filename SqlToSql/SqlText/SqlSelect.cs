using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using SqlToSql.Fluent;

namespace SqlToSql.SqlText
{
    public static class SqlSelect
    {

        static string SelectExpressionStr(Expression ex, Expression param)
        {
            if (ex is MemberExpression member && member.Expression == param)
            {
                return member.Member.Name;
            }
            return null;
        }

        static string SelectStr(LambdaExpression map)
        {
            var param = map.Parameters[0];
            var body = map.Body;
            string SubStr(Expression ex) => SelectExpressionStr(ex, param);
            string ToSql(Expression ex) => SqlExpression.ExprToSql(ex, SubStr);

            if (body is MemberInitExpression member)
            {
                return string.Join(", ",
                        member.Bindings.Cast<MemberAssignment>()
                        .Select(x => $"{ToSql(x.Expression)} AS \"{x.Member.Name}\"")
                    );
            }
            else if (body is NewExpression newExpr)
            {
                var typeProps = newExpr.Type.GetProperties().Select(x => x.Name).ToList();
                var consParams = newExpr.Constructor.GetParameters().Select(x => x.Name).ToList();

                return string.Join(", ",
                        newExpr.Arguments.Select((arg, i) => $"{ToSql(arg)} AS \"{typeProps.First(x => x.ToLower() == consParams[i].ToLower())}\"")
                    );
            }
            else if (body == param)
            {
                return "*";
            }

            return SelectExpressionStr(body, param);
        }

        public static string SelectToString<TIn, TOut>(SelectClause<TIn, TOut> clause)
        {
            var from = SqlFromList.FromListToStr(clause.From);


            var select = SelectStr(clause.Select);

            return $"SELECT {select}\r\n{from}";
        }
    }
}
