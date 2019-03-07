//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Text;
//using System.Threading.Tasks;

//namespace SqlToSql.Fluent
//{
//    public static class Sql2On
//    {
//        public class ConsParameter
//        {
//            public ConsParameter(string name, Expression expr)
//            {
//                Name = name;
//                Expr = expr;
//            }

//            public string Name { get; }
//            public Expression Expr { get; }
//        }

//        static IReadOnlyList<FromItemAlias> OnParams(IEnumerable<ConsParameter> args, Expression mapA, Expression mapB, IReadOnlyList<FromItemAlias> left, IFromListItem right)
//        {
//            var fromItems = args.Select((arg) =>
//            {
//                var par = arg.Expr;
//                var alias = arg.Name;

//                if (par == mapA)
//                {
//                    throw new ArgumentException("No se puede referenciar directamente a la parte izquierda del JOIN  en el metodo ON");
//                }
//                else if (par == mapB)
//                {
//                    return new FromItemAlias(right, alias);
//                }
//                else if (par is MemberExpression mem)
//                {
//                    if (mem.Expression != mapA)
//                        throw new ArgumentException("Los parametros deben de hacer referencia a una propiedad de lado izquierdo del JOIN, o al lado derecho del JOIN");
//                    var leftProp = left.Where(x => x.Alias == mem.Member.Name).FirstOrDefault();
//                    if (leftProp == null)
//                        throw new ArgumentException($"No se encontró ningun elemento en el from list del lado izquierdo '{mem.Member.Name}'");
//                    return new FromItemAlias(leftProp.Item, alias);
//                }
//                else
//                    throw new ArgumentException("Sólo se permiten expresiones en la siguiente forma: left.Prop O right");
//            });
//            return fromItems.ToList();
//        }

//        public static FromList<TOut> Alias<TIn, TOut>(this FromList<TIn> from, Expression<Func<TIn, TOut>> expr)
//        {
//            var dummy = Expression.Parameter(typeof(object));
//            var mapExpr = Expression.Lambda<Func<TIn, object, TOut>>(expr.Body, new[] { expr.Parameters[0], dummy, expr.Parameters[1] });

//            return On(
//                new JoinItems<TIn, object>(JoinType.Inner, from, null),
//                mapExpr
//                );
//        }
//        public static FromList<TRet> On<T1, T2, TRet>(JoinItems<T1, T2> items, Expression<Func<T1, T2, TRet>> map)
//        {
//            var mapA = map.Parameters[0];
//            var mapB = map.Parameters[1];
//            var cProps = map.Body.Type.GetProperties().Select(x => x.Name).ToList();
//            if (map.Body is NewExpression cons)
//            {
//                var consParams = cons.Constructor.GetParameters().Select(x => x.Name).ToList();
//                var args = cons.Arguments.ToList().Select((arg, i) =>
//                {
//                    var parName = consParams[i];
//                    var alias = cProps.Where(x => x.ToLower() == parName.ToLower()).FirstOrDefault();
//                    if (alias == null)
//                        throw new ArgumentException($"No se encontró ninguna propiedad en {cons.Type.Name} que encaje con el parametro {parName}");
//                    return new ConsParameter(alias, arg);
//                });

//                var fromItems = OnParams(args, mapA, mapB, items.Left.Clause.From, items.Right);
//                return new FromList<TRet>(fromItems);
//            }
//            else if (map.Body is MemberInitExpression mem)
//            {
//                var args = mem.Bindings.Select(arg =>
//                {
//                    if (arg is MemberAssignment asig)
//                    {
//                        return new ConsParameter(asig.Member.Name, asig.Expression);
//                    }
//                    else
//                        throw new ArgumentException($"En {arg.Member.Name} sólo se soporta asignación de miembros en el MemberInitExpression");
//                });

//                var fromItems = OnParams(args, mapA, mapB, items.Left.Clause.From, items.Right);
//                return new FromList<TRet>(fromItems);
//            }
//            else
//                throw new ArgumentException("La expresión debe de ser un constructor");
//        }
//    }
//}
