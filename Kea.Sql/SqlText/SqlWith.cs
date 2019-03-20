using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using KeaSql.ExprTree;
using KeaSql.Fluent;
using KeaSql.Fluent.Data;
using static KeaSql.ExprTree.ExprReplace;

namespace KeaSql.SqlText
{
    public static class SqlWith
    {
        /// <summary>
        /// Indica que una expresión se debe de sustituir con cierto nombre del WITH
        /// </summary>
        public class ExprWithAlias
        {
            public ExprWithAlias(Expression expr, string alias)
            {
                Expr = expr;
                Alias = alias;
            }

            public Expression Expr { get; }
            public string Alias { get; }
        }

        /// <summary>
        /// Resultado de analizar una claúsula WITH
        /// </summary>
        public class SqlWithTextResult
        {
            public SqlWithTextResult(string sqlSelect, string sqlUnion, string alias)
            {
                SqlSelect = sqlSelect;
                SqlUnion = sqlUnion;
                Alias = alias;
            }

            public string SqlSelect { get; }
            public string SqlUnion { get; }
            public string Alias { get; }
        }


        static Expression RawSqlTableRefExpr(Type sqlType, string sql)
        {
            var method = typeof(Sql).GetMethods().Where(x => x.Name == nameof(Sql.RawTableRef) && x.IsGenericMethod).Single();
            var mgen = method.MakeGenericMethod(sqlType);

            var ret = Expression.Call(mgen, Expression.Constant(sql));
            return ret;
        }

        /// <summary>
        /// Sustituye un subquery de un WITH
        /// </summary>
        public static Expression SubquerySubs(Expression subquery, ParameterExpression queryParam, IReadOnlyList<ExprRep> mapAliases, ParameterExpression mapLeftParam)
        {
            if (subquery == null) return null;

            //Sustituir el mapLeftParan en los mapAliases con el queryParam:
            var subs = mapAliases.Select(x => new ExprRep(ExprTree.ReplaceVisitor.Replace(x.Find, mapLeftParam, queryParam), x.Rep));

            var ret = ReplaceExprList(subquery, subs);
            return ret;
        }

        /// <summary>
        /// Remplaza todas las referencias a un elemento del WITH con un SqlTableRefRaw
        /// </summary>
        public static Expression SubqueryRawSubs(Expression subquery, ParameterExpression repParam)
        {
            if (subquery == null) return null;

            //Sustituir todo param.X o param por el nombre:
            var ret = ReplaceVisitor.Replace(subquery, expr =>
            {
                if (typeof(ISqlSelect).IsAssignableFrom(expr.Type))
                {
                    var selectInt = expr.Type.GetInterfaces().Concat(new[] { expr.Type }).Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ISqlSelect<>)).FirstOrDefault();
                    if (selectInt == null)
                    {
                        throw new ArgumentException("Debe de ser un ISqlSelect<T>");
                    }

                    var selectType = selectInt.GetGenericArguments()[0];

                    if (expr is MemberExpression mem && CompareExpr.ExprEquals(mem.Expression, repParam))
                    {
                        return RawSqlTableRefExpr(selectType, $"\"{mem.Member.Name}\"");
                    }
                    else if (CompareExpr.ExprEquals(expr, repParam))
                    {
                        return RawSqlTableRefExpr(selectType, $"\"{repParam.Name}\"");
                    }
                }
                return null;
            });

            return ret;
        }

        static string SelectToString(ISqlSelect select, ParamMode paramMode, SqlParamDic paramDic)
        {
            return $"(\r\n{SqlSelect.TabStr(SqlSelect.SelectToString(select.Clause, paramMode, paramDic))}\r\n)";
        }

        static string WithToString(string alias, ISqlSelect select, ISqlSelect recursive, SqlWithType type, ParamMode paramMode, SqlParamDic paramDic)
        {
            StringBuilder b = new StringBuilder();
            if (type != SqlWithType.Normal)
                b.Append("RECURSIVE ");

            b.Append(alias);
            b.AppendLine(" AS (");
            b.AppendLine(SqlSelect.TabStr(SqlSelect.SelectToString(select.Clause, paramMode, paramDic)));
            if (type != SqlWithType.Normal)
            {
                if (recursive == null)
                    throw new ArgumentNullException(nameof(recursive));

                b.AppendLine();
                b.AppendLine(
                    SqlSelect.TabStr(
                        type == SqlWithType.RecursiveUnion ? "UNION" :
                        type == SqlWithType.RecursiveUnionAll ? "UNION ALL" :
                        throw new ArgumentException(nameof(type))
                    )
                    );
                b.AppendLine();

                b.AppendLine(SqlSelect.TabStr(SqlSelect.SelectToString(recursive.Clause, paramMode, paramDic)));
            }
            b.Append(")");

            var ret = b.ToString();
            return ret;
        }

        public static ISqlSelect GetSelectFromExpr(Expression body)
        {
            if (body == null) return null;

            var lambda = Expression.Lambda(body, new ParameterExpression[0]);
            var comp = lambda.Compile();
            var exec = comp.DynamicInvoke(new object[0]);
            return (ISqlSelect)exec;
        }

        public static string WithToSql(ISqlWith with, ParameterExpression param, ParamMode paramMode, SqlParamDic paramDic)
        {
            return ApplyReplace(with, new ExprRep[0], null, param, paramMode, paramDic);
        }

        public static string ApplyReplace(
            ISqlWith with,
            IEnumerable<ExprTree.ExprReplace.ExprRep> replaces,
            Func<Expression, Expression> rawReplaces,
            ParameterExpression repParam,
            ParamMode paramMode, SqlParamDic paramDic
            )
        {
            var leftParam = with.Map.Parameters[0];
            var rightParam = with.Map.Parameters[1];
            var mapBody = with.Map.Body;

            if (ExprTree.CompareExpr.ExprEquals(with.Map.Body, rightParam))
            {
            }

            var subs = new List<ExprRep>();

            //Agrega las sustituciones del map:
            var mapAliases =
                (CompareExpr.ExprEquals(with.Map.Body, rightParam)) ? new[] { new ExpressionAlias(null, rightParam) } :
                ExprTree.ExprReplace.ExtractAliases(with.Map.Body);

            var mapAliasSubs = mapAliases.Select(x => new ExprRep(x.Expr, x.Alias == null ? (Expression)repParam : Expression.Property(repParam, x.Alias))).ToList();
            if(with.Recursive != null)
            {
                //Sustituir el segundo parametro del recursive, que es equivalente al B del map
                subs.Add(new ExprRep(with.Recursive.Parameters[1], rightParam));
            }

            subs.AddRange(mapAliasSubs);

            //Agregar las sustituciones de arriba:
            subs.AddRange(replaces);



            //Sustituir el SELECT y el UNION ALL:
            var selectSubs = SubquerySubs(with.Select.Body, with.Select.Parameters[0], subs, leftParam);
            var unionSubs = SubquerySubs(with.Recursive?.Body, with.Recursive?.Parameters[0], subs, leftParam);

            //Después de todas las sustituciones, sustituir por el SqlRaw:
            if (rawReplaces == null)
            {
                rawReplaces = x =>
                {
                    return SubqueryRawSubs(x, repParam);
                };
            }
            var selectSubRaw = rawReplaces(selectSubs);
            var unionSubRaw = rawReplaces(unionSubs);

            //El alias de este with es el nombre del segundo parametro del map:
            var rightAliasExpr = rawReplaces(ReplaceExprList(rightParam, subs));
            var rightAlias = (string)((ConstantExpression)((MethodCallExpression)(rightAliasExpr)).Arguments[0]).Value;

            var select = GetSelectFromExpr(selectSubRaw);
            var union = GetSelectFromExpr(unionSubRaw);

            var b = new StringBuilder();
            if (with.Left != null)
            {
                //Le pasamos al lado izquierdo los replaces del map actual:
                var subReps = subs.ToList();
                var lRet = ApplyReplace(with.Left, subReps, rawReplaces, leftParam, paramMode, paramDic);
                b.Append(lRet);
                b.Append(", ");
            }
            else
            {
                b.Append("WITH ");
            }
            var withText = WithToString(rightAlias, select, union, with.Type, paramMode, paramDic);
            b.Append(withText);
            var ret = b.ToString();

            return ret;
        }
    }
}
