using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SqlToSql.Fluent;

namespace SqlToSql.SqlText
{
    public static class SqlSelect
    {

        static string SelectExpressionStr(Expression ex, Expression param, bool fromListNamed)
        {
            if (ex is MemberExpression member && member.Expression == param)
            {
                return $"\"{member.Member.Name}\"";
            }
            return null;
        }

        static string SelectStr(LambdaExpression map, bool fromListNamed, string fromlistAlias)
        {
            var param = map.Parameters[0];
            var body = map.Body;
            string SubStr(Expression ex) => SelectExpressionStr(ex, param, fromListNamed);
            string ToSql(Expression ex) => SqlExpression.ExprToSql(ex, SubStr);

            string MemberAssigToSql(Expression expr, MemberInfo prop)
            {
                if (expr == param)
                {
                    return $"{fromlistAlias}.*";
                }
                if(fromListNamed && expr is MemberExpression mem && mem.Expression == param)
                {
                    return $"{ToSql(mem)}.*";
                }

                if (fromListNamed)
                {
                    return $"{ToSql(expr)} AS \"{prop.Name}\"";
                }
                return $"{fromlistAlias}.{ToSql(expr)} AS \"{prop.Name}\"";
            }

            if (body is MemberInitExpression member)
            {
                return string.Join(", ",
                        member.Bindings.Cast<MemberAssignment>()
                        .Select(x => MemberAssigToSql(x.Expression, x.Member))
                    );
            }
            else if (body is NewExpression newExpr)
            {
                var typeProps = newExpr.Type.GetProperties().ToList();
                var consParams = newExpr.Constructor.GetParameters().Select(x => x.Name).ToList();

                return string.Join(", ",
                        newExpr.Arguments.Select((arg, i) => MemberAssigToSql(arg, typeProps.First(x => x.Name.ToLower() == consParams[i].ToLower())))
                    );
            }
            else if (body == param)
            {
                return $"{fromlistAlias}.*";
            }

            return SelectExpressionStr(body, param, fromListNamed);
        }

        /// <summary>
        /// Convierte una cláusula de SELECT a string
        /// </summary>
        public static string SelectToString(ISelectClause clause)
        {
            return CallSelectToStringG(clause);
        }

        static string CallSelectToStringG(ISelectClause clause)
        {
            var types = clause.GetType().GetGenericArguments();
            var method = typeof(SqlSelect)
               .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
               .Where(x => x.Name == nameof(SelectToStringG))
               .First()
               .MakeGenericMethod(types)
               ;

            return (string)method.Invoke(null, new object[] { clause });
        }

        static string SelectToStringG<TIn, TOut>(SelectClause<TIn, TOut> clause)
        {
            var fromAlias = $"\"{clause.Select.Parameters[0].Name}\"" ;
            var from = SqlFromList.FromListToStr(clause.From, fromAlias);
            var select = SelectStr(clause.Select, from.Named, fromAlias);

            return $"SELECT {select}\r\n{from.Sql}";
        }
    }
}
