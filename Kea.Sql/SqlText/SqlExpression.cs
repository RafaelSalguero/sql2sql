using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using KeaSql.Fluent;
using KeaSql.SqlText.Rewrite;
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
        public SqlExprParams(ParameterExpression param, ParameterExpression window, bool fromListNamed, string fromListAlias, IReadOnlyList<ExprStrAlias> replace, ParamMode paramMode, SqlParamDic paramDic)
        {
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
        public static SqlExprParams Empty(ParamMode mode, SqlParamDic paramDic) => new SqlExprParams(null, null, false, "", new ExprStrAlias[0], mode, paramDic);

        public SqlExprParams SetPars(ParameterExpression param, ParameterExpression window) =>
            new SqlExprParams(param, window, FromListNamed, FromListAlias, Replace, ParamMode, ParamDic);

        public ParameterExpression Param { get; }
        public ParameterExpression Window { get; }
        public bool FromListNamed { get; }
        public string FromListAlias { get; }
        public IReadOnlyList<ExprStrAlias> Replace { get; }
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
            else if (call.Method.DeclaringType == typeof(SqlExtensions))
            {
                switch (call.Method.Name)
                {
                    case nameof(SqlExtensions.Scalar):
                        return SqlCalls.ScalarToSql(call, pars);
                }
                throw new ArgumentException("Para utilizar un subquery dentro de una expresión utilice la función SqlExtensions.Scalar");
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

        static string ConstToSql(object value)
        {
            if (value == null)
            {
                return "NULL";
            }
            var type = value.GetType();
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var val = ((dynamic)value).Value;
                return ConstToSql(val);
            }
            else if (typeInfo.IsEnum)
            {
                var member = typeInfo.DeclaredMembers.Where(x => x.Name == value.ToString()).FirstOrDefault();
                if (member != null && (member.GetCustomAttribute<SqlNameAttribute>() is var att) && att != null)
                {
                    return att.SqlName;
                }
                return ((int)value).ToString();
            }

            if (value is string || value is Guid)
            {
                return $"'{value}'";
            }
            else if (
                value is decimal || value is int || value is float || value is double || value is long || value is byte || value is sbyte ||
                value is bool
                )
            {
                return value.ToString();
            }
            else if (value is DateTime date)
            {
                if (date.Date - date == TimeSpan.Zero)
                {
                    //No tiene componente de horas
                    return $"'{date.ToString("yyyy-MM-dd")}'";
                }
                else
                {
                    return $"'{date.ToString("yyyy-MM-dd HH:mm:ss")}'";
                }
            }
            else if (value is DateTimeOffset dateOff)
            {
                var off = dateOff.Offset;
                var timeZoneOffset = (off < TimeSpan.Zero ? "-" : "+") + off.ToString("hh:mm");

                if (dateOff.LocalDateTime.Date - dateOff.LocalDateTime == TimeSpan.Zero)
                {
                    return $"'{dateOff.ToString("yyyy-MM-dd")} {timeZoneOffset}'";
                }
                else
                {
                    return $"'{dateOff.ToString("yyyy-MM-dd HH:mm:ss")} {timeZoneOffset}'";
                }
            }
            throw new ArgumentException($"No se puede convertir a SQL la constante " + value.ToString());
        }

        static string UnaryToSql(UnaryExpression un, SqlExprParams pars)
        {
            string ToStr(Expression ex) => ExprToSql(ex, pars, false);

            switch (un.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    return $"-({ToStr(un.Operand)})";
                case ExpressionType.Not:
                    return $"NOT ({ToStr(un.Operand)})";
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    return ToStr(un.Operand);
            }
            throw new ArgumentException($"No se pudo convertir a SQL la expresión unaria '{un}'");
        }

        static string BinaryToSql(BinaryExpression bin, SqlExprParams pars)
        {
            string ToStr(Expression ex) => ExprToSql(ex, pars, false);
            if (bin.Right is ConstantExpression conR && conR.Value == null)
            {
                if (bin.NodeType == ExpressionType.Equal)
                    return $"({ToStr(bin.Left)} IS NULL)";
                else if (bin.NodeType == ExpressionType.NotEqual)
                    return $"({ToStr(bin.Left)} IS NOT NULL)";
                else
                    throw new ArgumentException("No se puede convertir la expresión " + bin);
            }
            else if (bin.Left is ConstantExpression conL && conL.Value == null)
            {
                if (bin.NodeType == ExpressionType.Equal)
                    return $"({ToStr(bin.Right)} IS NULL)";
                else if (bin.NodeType == ExpressionType.NotEqual)
                    return $"({ToStr(bin.Right)} IS NOT NULL)";
                else
                    throw new ArgumentException("No se puede convertir la expresión " + bin);
            }

            //Note que los operadores = y != de C# se comportan como el 
            //IS DISTINCT FROM de postgres, y no como los operadores de postgres
            var ops = new Dictionary<ExpressionType, string>
                {
                    { ExpressionType.Add, "+" },
                    { ExpressionType.AddChecked, "+" },

                    { ExpressionType.Subtract, "-" },
                    { ExpressionType.SubtractChecked, "-" },

                    { ExpressionType.Multiply, "*" },
                    { ExpressionType.MultiplyChecked, "*" },

                    { ExpressionType.Divide, "/" },

                    { ExpressionType.Equal, "=" },
                    { ExpressionType.NotEqual, "!=" },
                    { ExpressionType.GreaterThan, ">" },
                    { ExpressionType.GreaterThanOrEqual, ">=" },
                    { ExpressionType.LessThan, "<" },
                    { ExpressionType.LessThanOrEqual, "<=" },

                    { ExpressionType.AndAlso, "AND" },
                    { ExpressionType.OrElse, "OR" },
                };

            if (ops.TryGetValue(bin.NodeType, out string opStr))
            {
                var esString = bin.Left.Type == typeof(string) || bin.Right.Type == typeof(string);

                //La concatenación de cadenas es con el operador '||'
                if (opStr == "+" && esString)
                {
                    opStr = "||";
                }
                return $"({ToStr(bin.Left)} {opStr} {ToStr(bin.Right)})";
            }
            throw new ArgumentException("No se pudo convertir a SQL la expresión binaria" + bin);
        }

        static string ParamToSql(SqlParamItem param, ParamMode mode)
        {
            switch (mode)
            {
                case ParamMode.EntityFramework:
                    return $"@{param.ParamName}";
                case ParamMode.Substitute:
                    return ConstToSql(param.GetValue());
                default:
                    throw new ArgumentException("Parma mode");
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

        static bool IsNullableType(Type t) => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        static bool IsNullableMember(MemberExpression mem) => IsNullableType(mem.Expression.Type);

        static string NullableMemberToSql(MemberExpression mem, SqlExprParams pars)
        {
            var memSql = ExprToSql(mem.Expression, pars, false);
            if (mem.Member.Name == nameof(Nullable<int>.Value))
            {
                return memSql;
            }
            else if (mem.Member.Name == nameof(Nullable<int>.HasValue))
            {
                return $"(({memSql}) IS NOT NULL)";
            }

            throw new ArgumentException($"El miembro '{mem.Member.Name}' no se reconocio para el tipo nullable");
        }

        public static bool IsComplexType(Type t)
        {
            var attNames = t.CustomAttributes.Select(x => x.AttributeType).Select(x => x.Name);
            return attNames.Contains("ComplexTypeAttribute") || attNames.Contains("OwnedAttribute");
        }


        /// <summary>
        /// Obtiene todas las subrutas de un tipo, en caso de ser un ComplexType tendra un arreglo con las sub rutas, si no, tendra un arreglo con una cadena vacía como único elemento
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static IReadOnlyList<string> SubPaths(Type type)
        {
            if (!IsComplexType(type))
            {
                return new[] { "" };
            }

            var props = type.GetProperties().Select(x => new
            {
                prop = x,
                complex = IsComplexType(x.PropertyType)
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

        static string SingleMemberToSql(SqlExprParams pars, string baseMemberName, string subpath, MemberExpression mem)
        {
            var memberName = baseMemberName + subpath;
            if (pars.FromListNamed)
            {
                MemberExpression firstExpr = mem;
                while (firstExpr is MemberExpression sm1 && sm1.Expression is MemberExpression sm2)
                {
                    firstExpr = sm2;
                }

                if (IsFromParam( mem.Expression ))
                {
                    throw new ArgumentException("No esta soportado obtener una expresión de * en el SingleMemberSql");
                }
                else if (IsFromParam( firstExpr.Expression ))
                {
                    return $"\"{firstExpr.Member.Name}\".\"{memberName}\"";
                }
                else if (IsRawTableRef(firstExpr.Expression, out var raw))
                {
                    return $"{raw}.\"{memberName}\"";
                }
            }
            else
            {
                Expression firstExpr = mem;
                while (firstExpr is MemberExpression sm)
                {
                    firstExpr = sm.Expression;
                }

                if (IsFromParam( firstExpr ))
                {
                    return $"{pars.FromListAlias}.\"{memberName}\"";
                }
                else if (IsRawTableRef(firstExpr, out var raw))
                {
                    return $"{raw}.\"{memberName}\"";
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

            //Si es un miembro del nullable:
            if (IsNullableMember(mem))
            {
                return (SqlSubpath.FromString(NullableMemberToSql(mem, pars)), false);
            }

            //Si el tipo es un complex type:
            var subpaths = SubPaths(mem.Type);
            string memberName = mem.Member.Name;
            if (IsComplexType(mem.Expression.Type) && mem.Expression is MemberExpression)
            {
                var complexName = new List<string>();
                MemberExpression curr = mem;
                complexName.Add(mem.Member.Name);
                while (IsComplexType(curr.Expression.Type) && curr.Expression is MemberExpression m2)
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
                switch (mem.Member.Name)
                {
                    case nameof(string.Length):
                        return (SqlSubpath.FromString($"char_length({ExprToSql(mem.Expression, pars, false)})"), false);
                    default:
                        throw new ArgumentException($"No se pudo convertir a SQL el miembro de 'string' '{mem.Member.Name}'");
                }
            }

            //Estrella:
            if (pars.FromListNamed && IsFromParam( mem.Expression ))
            {
                return (SqlSubpath.FromString($"\"{mem.Member.Name}\".*"), true);
            }

            var items = subpaths.Select(x => new SqlSubpath(SingleMemberToSql(pars, memberName, x, mem), x)).ToList();
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

                return (SqlSubpath.FromString($"{pars.FromListAlias}.*"), true);
            }

            var replace = SqlFromList.ReplaceStringAliasMembers(expr, pars.Replace);
            if (replace != null) return (SqlSubpath.FromString(replace), false);

            string ToStr(Expression ex) => ExprToSql(ex, pars, false);

            if (expr is BinaryExpression bin)
            {
                return (SqlSubpath.FromString(BinaryToSql(bin, pars)), false);
            }

            else if (expr is MemberExpression mem)
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
            else if (expr is ConstantExpression cons)
            {
                return (SqlSubpath.FromString(ConstToSql(cons.Value)), false);
            }
            else if (expr is UnaryExpression un)
            {
                return (SqlSubpath.FromString(UnaryToSql(un, pars)), false);
            }
            throw new ArgumentException("No se pudo convertir a SQL la expresión " + expr.ToString());
        }
    }
}
