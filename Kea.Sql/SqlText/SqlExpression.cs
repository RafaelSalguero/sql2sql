using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using KeaSql.Fluent;
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
                var args = string.Join(", ", call.Arguments.Select(x => ExprToSql(x, pars)));
                return $"{funcAtt.SqlName}({args})";
            }
            else if (call.Method.DeclaringType == typeof(Sql))
            {
                switch (call.Method.Name)
                {
                    case nameof(Sql.Raw):
                    case nameof(Sql.RawRowRef):
                        return SqlCalls.RawToSql(call, pars);
                    case nameof(Sql.Over):
                        return SqlCalls.OverToSql(call, pars);
                    case nameof(Sql.Filter):
                        return SqlCalls.FilterToSql(call, pars);
                    case nameof(Sql.Between):
                        return SqlCalls.BetweenToSql(call, pars);
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

        public static string ExprToSql(Expression expr, SqlExprParams pars)
        {
            return ExprToSqlStar(expr, pars).sql;
        }



        public static string ConditionalToSql(ConditionalExpression expr, SqlExprParams pars)
        {
            var b = new StringBuilder();

            Expression curr = expr;

            while (curr is ConditionalExpression cond)
            {
                b.Append("WHEN ");
                b.Append(ExprToSql(cond.Test, pars));
                b.Append(" THEN ");
                b.Append(ExprToSql(cond.IfTrue, pars));

                b.AppendLine();
                curr = cond.IfFalse;
            }

            b.Append("ELSE ");
            b.Append(ExprToSql(curr, pars));

            return SqlSelect.TabStr($"\r\nCASE\r\n{SqlSelect.TabStr(b.ToString())}\r\nEND");
        }

        static string ConstToSql(object value)
        {
            if (value == null)
            {
                return "NULL";
            }
            var type = value.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var val = ((dynamic)value).Value;
                return ConstToSql(val);
            }
            else if (type.IsEnum)
            {
                var member = type.GetMember(value.ToString()).FirstOrDefault();
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
            string ToStr(Expression ex) => ExprToSql(ex, pars);

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
            string ToStr(Expression ex) => ExprToSql(ex, pars);
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
        /// Devuelve el numero del parametro si es que es un parametro, si no, devuelve null
        /// </summary>
        static SqlParamItem IsParam(MemberExpression mem, SqlParamDic dic)
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

                return dic.AddParam(target, members);
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

        static (string sql, bool star) MemberToSql(MemberExpression mem, SqlExprParams pars)
        {
            if (IsParam(mem, pars.ParamDic) is var param && param != null)
            {
                return (ParamToSql(param, pars.ParamMode), false);
            }

            if (pars.FromListNamed)
            {
                MemberExpression firstExpr = mem;
                while (firstExpr is MemberExpression sm1 && sm1.Expression is MemberExpression sm2)
                {
                    firstExpr = sm2;
                }

                if (mem.Expression == pars.Param)
                {
                    return ($"\"{mem.Member.Name}\".*", true);
                }
                else if (firstExpr.Expression == pars.Param)
                {
                    return ($"\"{firstExpr.Member.Name}\".\"{mem.Member.Name}\"", false);
                }
                else if (IsRawTableRef(firstExpr.Expression, out var raw))
                {
                    return ($"{raw}.\"{mem.Member.Name}\"", false);
                }
            }
            else
            {
                Expression firstExpr = mem;
                while (firstExpr is MemberExpression sm)
                {
                    firstExpr = sm.Expression;
                }

                if (firstExpr == pars.Param)
                {
                    return ($"{pars.FromListAlias}.\"{mem.Member.Name}\"", false);
                }
                else if (IsRawTableRef(firstExpr, out var raw))
                {
                    return ($"{raw}.\"{mem.Member.Name}\"", false);
                }
            }

            //Intentamos convertir al Expr a string con el replace:
            var exprRep = SqlFromList.ReplaceStringAliasMembers(mem.Expression, pars.Replace);
            if (exprRep != null)
            {
                return ($"{exprRep}.\"{mem.Member.Name}\"", false);
            }

            var exprStr = ExprToSql(mem.Expression, pars);
            return ($"{exprStr}.\"{mem.Member.Name}\"", false);
        }

        /// <summary>
        /// Convierte una expresión a SQL
        /// </summary>
        public static (string sql, bool star) ExprToSqlStar(Expression expr, SqlExprParams pars)
        {
            //Es importante primero comprobar la igualdad del parametro, ya que el replace list tiene una entrada para el parametro tambien
            if (expr == pars.Param)
            {
                if (pars.FromListNamed)
                {
                    return ($"*", true);
                }

                return ($"{pars.FromListAlias}.*", true);
            }

            var replace = SqlFromList.ReplaceStringAliasMembers(expr, pars.Replace);
            if (replace != null) return (replace, false);

            string ToStr(Expression ex) => ExprToSql(ex, pars);

            if (expr is BinaryExpression bin)
            {
                return (BinaryToSql(bin, pars), false);
            }

            else if (expr is MemberExpression mem)
            {
                return MemberToSql(mem, pars);
            }
            else if (expr is ConditionalExpression cond)
            {
                return (ConditionalToSql(cond, pars), false);
            }
            else if (expr is MethodCallExpression call)
            {
                return (CallToSql(call, pars), false);
            }
            else if (expr is ConstantExpression cons)
            {
                return (ConstToSql(cons.Value), false);
            }
            else if (expr is UnaryExpression un)
            {
                return (UnaryToSql(un, pars), false);
            }
            throw new ArgumentException("No se pudo convertir a SQL la expresión " + expr.ToString());
        }
    }
}
