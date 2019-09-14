using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using KeaSql.Fluent;
using KeaSql.SqlText.Rewrite;
using KeaSql.SqlText.Rewrite.Rules;
using static KeaSql.SqlText.SqlFromList;

namespace KeaSql.SqlText
{
    /// <summary>
    /// Indica el modo en el que se van a poner los parametros en el SQL generado
    /// </summary>
    public enum ParamMode
    {
        /// <summary>
        /// Los parámetros estan deshabilitados
        /// </summary>
        None,

        /// <summary>
        /// Es @paramName
        /// </summary>
        EntityFramework,

        /// <summary>
        /// En lugar de poner los parámetros pone los valores directo en el SQL, esto permite ejecutar tal cual el SQL
        /// </summary>
        Substitute
    }

    public class SqlExprParams
    {
        public SqlExprParams(ParameterExpression param, ParameterExpression window, bool fromListNamed, string fromListAlias, IReadOnlyList<ExprStrRawSql> replace, ParamMode paramMode, SqlParamDic paramDic)
        {
            if(replace.Count > 0)
            {
                ;
            }
            if (fromListNamed && fromListAlias != null)
                throw new ArgumentException($"'{nameof(fromListAlias)}' debe de ser null cuando '{nameof(fromListNamed)}' = true");
            if (fromListAlias == "")
                throw new ArgumentException($"'{nameof(fromListAlias)}' no debe de ser una cadena vacía");

            Param = param;
            Window = window;
            FromListNamed = fromListNamed;
            FromListAlias = fromListAlias;
            Replace = replace;
            ParamMode = paramMode;
            ParamDic = paramDic;
        }

        /// <summary>
        /// Empty sin parámetros
        /// </summary>
        public static SqlExprParams EmptySP => Empty(ParamMode.None, new SqlParamDic());
        public static SqlExprParams Empty(ParamMode mode, SqlParamDic paramDic) => new SqlExprParams(null, null, false, null, new ExprStrRawSql[0], mode, paramDic);


        /// <summary>
        /// Reemplaza el parametro del select tanto en el campo Param como en la lista de Replace
        /// </summary>
        public SqlExprParams ReplaceSelectParams(ParameterExpression newParam, ParameterExpression newWindow)
        {
            var repList = Replace.Select(x =>
                x.Expr == this.Param ? new ExprStrRawSql(newParam, x.Sql) :
                x.Expr == this.Window ? new ExprStrRawSql(newWindow, x.Sql) :
                x
            ).ToList();

            return new SqlExprParams(
                newParam,
                newWindow,
                this.FromListNamed,
                this.FromListAlias,
                repList,
                this.ParamMode,
                this.ParamDic
                );
        }

        /// <summary>
        /// Parámetro que se considera ya sea la tabla del FROM si <see cref="FromListNamed"/> = false 
        /// o el objetos de aliases de la lista de FROM si <see cref="FromListNamed"/> = true
        /// </summary>
        public ParameterExpression Param { get; }

        /// <summary>
        /// Parámetro que representa al objeto de los WINDOWs
        /// </summary>
        public ParameterExpression Window { get; }

        /// <summary>
        /// Si el FROM list tiene aliases, esto indica que el parametro del SELECT no hace referencia a la tabla directamente, si no
        /// que hace referencia al objeto de aliases. Esto afecta a la forma de convertir a SQL las expresiones que son acceso a miembros
        ///  del parámetro del SELECT
        /// </summary>
        public bool FromListNamed { get; }

        /// <summary>
        /// Alias del FROM, sólo aplica cuando <see cref="FromListNamed"/> = false ya que de otra forma cada elemento del FROM list
        /// tiene su propio alias.
        /// 
        /// Note que este alias no es SQL, así que aquí no lleva comillas los nombres (para posgres)
        /// 
        /// En caso de que <see cref="FromListAlias"/> sea null, no se pondra el calificador del FROM en los miembros.
        /// Ej. En lugar de poner "Cliente"."Nombre" se pondrá solamente "Nombre"
        /// 
        /// No esta permitido que sea una cadena vacía.
        /// </summary>
        public string FromListAlias { get; }
        public IReadOnlyList<ExprStrRawSql> Replace { get; }
        public ParamMode ParamMode { get; }
        public SqlParamDic ParamDic { get; }
    }

    public static class SqlExpression
    {
        static string CallToSql(MethodCallExpression call, SqlExprParams pars)
        {
            var funcAtt = call.Method.GetCustomAttribute<SqlNameAttribute>();
            if (funcAtt != null)
            {
                var args = string.Join(", ", call.Arguments.Select(x => ExprToSql(x, pars, false)));
                return $"{funcAtt.SqlName}({args})";
            }
            else if (call.Method.DeclaringType == typeof(Sql))
            {
                switch (call.Method.Name)
                {
                    case nameof(Sql.Raw):
                    case nameof(Sql.RawRowRef):
                        return SqlCalls.RawToSql(call, pars);
                }
            }
            else if (call.Method.DeclaringType == typeof(SqlSelectExtensions))
            {
                switch (call.Method.Name)
                {
                    case nameof(SqlSelectExtensions.Scalar):
                        return SqlCalls.ScalarToSql(call, pars);
                    default:
                        //Si es una llamada a las extensiones y no es la llamada a Scalar entonces es un subquery:
                        //Por ejemplo, uno dentro de una expresión EXISTS o IN
                        return SqlCalls.SubqueryToSql(call, pars);
                }
            }

            throw new ArgumentException("No se pudo convertir a SQL la llamada a la función " + call);
        }

        public static string ExprToSql(Expression expr, SqlExprParams pars, bool rewrite)
        {
            return SqlSubpath.Single(ExprToSqlStar(expr, pars, rewrite).sql);
        }

        public static string ConditionalToSql(ConditionalExpression expr, SqlExprParams pars)
        {
            var b = new StringBuilder();

            Expression curr = expr;

            while (curr is ConditionalExpression cond)
            {
                b.Append("WHEN ");
                b.Append(ExprToSql(cond.Test, pars, false));
                b.Append(" THEN ");
                b.Append(ExprToSql(cond.IfTrue, pars, false));

                b.AppendLine();
                curr = cond.IfFalse;
            }

            b.Append("ELSE ");
            b.Append(ExprToSql(curr, pars, false));

            return SqlSelect.TabStr($"\r\nCASE\r\n{SqlSelect.TabStr(b.ToString())}\r\nEND");
        }



        static string ParamToSql(SqlParamItem param, ParamMode mode)
        {
            switch (mode)
            {
                case ParamMode.EntityFramework:
                    return $"@{param.ParamName}";
                case ParamMode.Substitute:
                    return SqlConst.ConstToSql(param.GetValue());
                default:
                    throw new ArgumentException("No se pueden usar parametros con el param mode 'None'");
            }
        }


        /// <summary>
        /// Devuelve el el target y el path si es que la expresión es un parámetro, si no, devuelve null
        /// </summary>
        public static (object target, IReadOnlyList<MemberInfo> members)? IsParam(MemberExpression mem, SqlParamDic dic)
        {
            var first = mem;
            var members = new List<MemberInfo>();
            while (first.Expression is MemberExpression leftMem)
            {
                members.Add(first.Member);
                first = leftMem;
            }

            members.Add(first.Member);

            //Poner primero los miembros de hasta la izquierda:
            members.Reverse();

            if (first.Expression is ConstantExpression cons)
            {
                if (!cons.Type.Name.StartsWith("<>c__DisplayClass"))
                    return null;

                var target = cons.Value;

                if (typeof(IEnumerable).IsAssignableFrom(mem.Type) && mem.Type != typeof(string))
                {
                    throw new ArgumentException($"No se pueden parametrizar la expresión '{mem}' ya que es una colección");
                }
                return (target, members);
            }
            return null;
        }


        /// <summary>
        /// Devuelve el numero del parametro si es que es un parametro, si no, devuelve null
        /// </summary>
        static SqlParamItem AddParam(MemberExpression mem, SqlParamDic dic)
        {
            var path = IsParam(mem, dic);
            if (path != null)
            {
                return dic.AddParam(path.Value.target, path.Value.members);
            }

            return null;
        }

        /// <summary>
        /// En caso de que la expresión sea un RawTableRef, devuelve la referencia, si no, devuelve null
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        static bool IsRawTableRef(Expression ex, out string val)
        {
            if (
                ex is MethodCallExpression m &&
                m.Method.DeclaringType == typeof(Sql) &&
                m.Method.Name == nameof(Sql.RawRowRef)
                )
            {
                if (m.Arguments[0] is ConstantExpression cons)
                {
                    val = (string)cons.Value;
                    return true;
                }
                else
                    throw new ArgumentException("Un RawTableRef debe de tener una cadena constante");
            }
            val = null;
            return false;
        }



        /// <summary>
        /// Obtiene todas las subrutas de un tipo, en caso de ser un ComplexType tendra un arreglo con las sub rutas, si no, tendra un arreglo con una cadena vacía como único elemento
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static IReadOnlyList<string> SubPaths(Type type)
        {
            if (!Sql2Sql.Mapper.PathAccessor.IsComplexType(type))
            {
                return new[] { "" };
            }

            var props = type.GetProperties().Select(x => new
            {
                prop = x,
                complex = Sql2Sql.Mapper.PathAccessor.IsComplexType(x.PropertyType)
            });
            var simples = props.Where(x => !x.complex).Select(x => x.prop);
            var complex = props.Where(x => x.complex).Select(x => x.prop);

            var simplePaths = simples.Select(x => "_" + x.Name);
            var complexPaths = complex.SelectMany(prop =>
                SubPaths(prop.PropertyType).Select(x => "_" + prop.Name + x)
                );

            var allPaths = simplePaths.Concat(complexPaths);
            return allPaths.ToList();
        }

        /// <summary>
        /// Obtiene el SQL de una referencia a una columna de una tabla.
        /// </summary>
        /// <param name="table">Si es null se devuelve sólo la columna sin el identificador de la tabla</param>
        /// <param name="column">Nombre de la columna. Puede ser * para indicar todas las columnas</param>
        static string TableRefToSql(string table, string column)
        {
            var colSql = SqlSelect.ColNameToStr(column);
            var tableRaw = table != null ? $"\"{table}\"" : null;
            return RawTableRefToSql(tableRaw, colSql);
        }

        /// <summary>
        /// Obtiene el SQL de una referencia a una columna de una tabla.
        /// </summary>
        /// <param name="tableSql">Si es null se devuelve sólo la columna sin el identificador de la tabla. Es el SQL de la tabla</param>
        /// <param name="columnSql">SQL de la columna</param>
        static string RawTableRefToSql(string tableSql, string columnSql)
        {
            if (tableSql == null)
                return columnSql;
            else
                return $"{tableSql}.{columnSql}";
        }

        /// <summary>
        /// Convierte un <see cref="MemberExpression"/> a SQL, tomando en cuenta los aliases de la lista de froms 
        /// y la lógica descrita en <see cref="SqlExprParams"/>
        /// </summary>
        static string SingleMemberToSql(SqlExprParams pars, string baseMemberName, string subpath, MemberExpression mem)
        {
            var memberName = baseMemberName + subpath;
            if (pars.FromListNamed)
            {
                //Si la lista de FROM tiene aliases, el parametro del select no hace referencia a una tabla,
                //si no a un objeto de aliases donde cada propiedad es una tabla o un elemento de un JOIN
                MemberExpression firstExpr = mem;
                while (firstExpr is MemberExpression sm1 && sm1.Expression is MemberExpression sm2)
                {
                    firstExpr = sm2;
                }

                if (IsFromParam(mem.Expression))
                {
                    throw new ArgumentException("No esta soportado obtener una expresión de * en el SingleMemberSql");
                }
                else if (IsFromParam(firstExpr.Expression))
                {
                    return TableRefToSql(firstExpr.Member.Name, memberName);
                }
                else if (IsRawTableRef(firstExpr.Expression, out var raw))
                {
                    return RawTableRefToSql(raw, SqlSelect.ColNameToStr(memberName));
                }
            }
            else
            {
                //Si la lista de FROM no tiene aliases, el parámetro del SELECT hace referencia a la tabla del SELECT

                Expression firstExpr = mem;
                while (firstExpr is MemberExpression sm)
                {
                    firstExpr = sm.Expression;
                }

                if (IsFromParam(firstExpr))
                {
                    return TableRefToSql(pars.FromListAlias, memberName);
                }
                else if (IsRawTableRef(firstExpr, out var raw))
                {
                    return RawTableRefToSql(raw, SqlSelect.ColNameToStr(memberName));
                }
            }

            //Intentamos convertir al Expr a string con el replace:
            var exprRep = SqlFromList.ReplaceStringAliasMembers(mem.Expression, pars.Replace);
            if (exprRep != null)
            {
                return $"{exprRep}.\"{memberName}\"";
            }

            var exprStr = ExprToSql(mem.Expression, pars, false);
            return $"{exprStr}.\"{memberName}\"";
        }

        public class SqlSubpath
        {
            public SqlSubpath(string sql, string subpath)
            {
                Sql = sql;
                Subpath = subpath;
            }

            public string Sql { get; }
            public string Subpath { get; }

            public static string Single(IEnumerable<SqlSubpath> subpaths)
            {
                if (subpaths.Count() != 1)
                    throw new ArgumentException("No se permiten multiples subpaths en esta expresión");
                return subpaths.Single().Sql;
            }
            public static SqlSubpath[] FromString(string s) => new[] { new SqlSubpath(s, "") };
        }

        static (IReadOnlyList<SqlSubpath> sql, bool star) MemberToSql(MemberExpression mem, SqlExprParams pars)
        {
            //Si es un parametro:
            if (AddParam(mem, pars.ParamDic) is var param && param != null)
            {
                return (SqlSubpath.FromString(ParamToSql(param, pars.ParamMode)), false);
            }

            //Si el tipo es un complex type:
            var subpaths = SubPaths(mem.Type);
            string memberName = mem.Member.Name;
            if (Sql2Sql.Mapper.PathAccessor.IsComplexType(mem.Expression.Type) && mem.Expression is MemberExpression)
            {
                var complexName = new List<string>();
                MemberExpression curr = mem;
                complexName.Add(mem.Member.Name);
                while (Sql2Sql.Mapper.PathAccessor.IsComplexType(curr.Expression.Type) && curr.Expression is MemberExpression m2)
                {
                    curr = m2;
                    complexName.Add(curr.Member.Name);
                }

                complexName.Reverse();
                var name = string.Join("_", complexName);

                mem = curr;
                memberName = name;
            }


            //Miembros de string:
            if (mem.Expression.Type == typeof(string))
            {
                throw new ArgumentException($"No se pudo convertir a SQL el miembro de 'string' '{mem.Member.Name}'");
            }

            //Estrella:
            if (pars.FromListNamed && IsFromParam(mem.Expression))
            {
                return (SqlSubpath.FromString($"\"{mem.Member.Name}\".*"), true);
            }

            var items = subpaths.Select(x => new SqlSubpath(SingleMemberToSql(pars, memberName, x, mem), x)).ToList();
            if (subpaths.Count > 1)
            {
                ;
            }
            return (items, false);
        }

        static bool IsFromParam(Expression expr)
        {
            return expr is MethodCallExpression exprM && exprM.Method.DeclaringType == typeof(Sql) && exprM.Method.Name == nameof(Sql.FromParam);
        }



        /// <summary>
        /// Convierte una expresión a SQL
        /// </summary>
        public static (IReadOnlyList<SqlSubpath> sql, bool star) ExprToSqlStar(Expression expr, SqlExprParams pars, bool rewrite)
        {
            if (rewrite)
            {
                var visitor = new SqlRewriteVisitor(pars);
                expr = visitor.Visit(expr);
            }
            //Es importante primero comprobar la igualdad del parametro, ya que el replace list tiene una entrada para el parametro tambien
            if (IsFromParam(expr))
            {
                if (pars.FromListNamed)
                {
                    return (SqlSubpath.FromString($"*"), true);
                }

                return (SqlSubpath.FromString(SqlExpression.TableRefToSql(pars.FromListAlias, "*")), true);
            }

            var replace = SqlFromList.ReplaceStringAliasMembers(expr, pars.Replace);
            if (replace != null) return (SqlSubpath.FromString(replace), false);

            if (expr is MemberExpression mem)
            {
                return MemberToSql(mem, pars);
            }
            else if (expr is ConditionalExpression cond)
            {
                return (SqlSubpath.FromString(ConditionalToSql(cond, pars)), false);
            }
            else if (expr is MethodCallExpression call)
            {
                return (SqlSubpath.FromString(CallToSql(call, pars)), false);
            }
            else if (expr is MemberInitExpression || expr is NewExpression)
            {
                //TODO: Note la similaridad de este código, del InsertToString y del SelectStr
                //Puede ser que estas 3 funciones sean lógicamente equivalentes y que se puedan unificar

                var exprs = SqlSelect
                    .ExtractInitExpr(expr)
                    .Select(x => (x.mem, sql: ExprToSqlStar(x.expr, pars, false)));
                ;

                if (exprs.Any(y => y.sql.star))
                    throw new ArgumentException("No esta soportado una expresión star '*' en una subexpresión");

                var subpaths = exprs
                    .SelectMany(x => x.sql.sql, (parent, child) => (member: parent.mem, subpath: child))
                    .Select(x => new SqlSubpath(x.subpath.Sql, "_" + x.member.Name + x.subpath.Subpath))
                    .ToList()
                    ;

                return (subpaths, false);
            }
            throw new ArgumentException("No se pudo convertir a SQL la expresión " + expr.ToString());
        }
    }
}
