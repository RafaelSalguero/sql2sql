using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Mapper.ILCtors
{
    /// <summary>
    /// Logic related to IL dynamic code generation
    /// </summary>
    static class ILCtorLogic
    {
        /// <summary>
        /// A mapping between a property type and a IDataReader 
        /// </summary>
        class PropTypeMapping
        {
            public PropTypeMapping(string methodName, bool acceptsNull, SpecialPropTypeMapping special)
            {
                MethodName = methodName;
                AcceptsNull = acceptsNull;
                Special = special;
            }

            public string MethodName { get; }
            public bool AcceptsNull { get; }
            public SpecialPropTypeMapping Special { get; }
        }

        public enum SpecialPropTypeMapping
        {
            None,
            DateTimeOffset,
            ByteArray,
        }

        /// <summary>
        /// Returns the IDataReader.GetXXX method name and if the property accepts null
        /// </summary>
        static PropTypeMapping GetPropTypeMapping(Type type)
        {
            var isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
            var acceptsNull = isNullable || type.IsClass;

            var plainType = isNullable ? type.GetGenericArguments()[0] : type;
            var methodName =
                    plainType == typeof(bool) ? nameof(IDataReader.GetBoolean) :
                    plainType == typeof(int) ? nameof(IDataReader.GetInt32) :
                    plainType == typeof(short) ? nameof(IDataReader.GetInt16) :
                    plainType == typeof(long) ? nameof(IDataReader.GetInt64) :
                    plainType == typeof(decimal) ? nameof(IDataReader.GetDecimal) :
                    plainType == typeof(float) ? nameof(IDataReader.GetFloat) :
                    plainType == typeof(double) ? nameof(IDataReader.GetDouble) :
                    plainType == typeof(string) ? nameof(IDataReader.GetString) :
                    plainType == typeof(char) ? nameof(IDataReader.GetChar) :
                    plainType == typeof(DateTime) ? nameof(IDataReader.GetDateTime) :
                    //NOTE: DateTimeOffset needs special treatment
                    (
                        plainType == typeof(DateTimeOffset) ||
                        plainType == typeof(byte[])
                    ) ? (string)null : throw new ArgumentException($"Type '{type}' not supported in prop type mapping");

            var special =
                plainType == typeof(DateTimeOffset) ? SpecialPropTypeMapping.DateTimeOffset :
                plainType == typeof(byte[]) ? SpecialPropTypeMapping.ByteArray : SpecialPropTypeMapping.None;

            return new PropTypeMapping(methodName, acceptsNull, special);
        }

        /// <summary>
        /// Generates an expression that sets a given property with a value from the given data reader
        /// </summary>
        internal static Expression GeneratePropSet(Expression instance, Expression reader, PropertyInfo prop, int colIndex)
        {
            var typeMap = GetPropTypeMapping(prop.PropertyType);
            var readerType = reader.Type;

            Expression indexExpr = Expression.Constant(colIndex);
            Expression readExpr = Expression.Call(reader, typeMap.MethodName, new Type[0], indexExpr);
            Expression isDbNullExpr = Expression.Call(reader, nameof(IDataReader.IsDBNull), new Type[0], indexExpr);

            Expression readCondExpr =
                    !typeMap.AcceptsNull ? readExpr : Expression.Condition(isDbNullExpr, Expression.Constant(null, readExpr.Type), readExpr);

            Expression assigExpr = Expression.Assign(Expression.Property(instance, prop), readExpr);
            return assigExpr;
        }

        /// <summary>
        /// Generate the constructor call for creating the item instance
        /// </summary>
        internal static Expression GenerateConsCall(Expression reader, CtorMapping mapping)
        {
            return Expression.New(mapping.Constructor);

        }

        public static Expression GenerateLoopBody(Expression reader, Expression list, CtorMapping mapping)
        {
            var itemType = mapping.Constructor.DeclaringType;
            var body = new List<Expression>();

            var itemVar = Expression.Variable(itemType, "item");
            body.Add(Expression.Assign(itemVar, GenerateConsCall(reader, mapping)));
            body.AddRange(mapping.PropertyMapping.Select(x => GeneratePropSet(itemVar, reader, x.Key, x.Value)));
            body.Add(Expression.Call(list, "Add", new Type[0], itemVar));

            var ret = Expression.Block(
                new[] { itemVar },
                body.ToList()
                );
            return ret;
        }

        /// <summary>
        /// Generate the method body for reading a IDataReader
        /// </summary>
        static Expression GenerateMethodBody(Expression reader, CtorMapping mapping)
        {
            var br = Expression.Label("break");
            var itemType = mapping.Constructor.DeclaringType;
            var listType = typeof(List<>).MakeGenericType(itemType);

            var list = Expression.Variable(listType, "items");
            var listInit = Expression.Assign(list, Expression.New(listType));

            var loopCond = Expression.IfThen(
                Expression.Not(Expression.Call(reader, "Read", new Type[0])),
                Expression.Break(br)
                );

            var loop = Expression.Loop(
                Expression.Block(
                    loopCond,
                    GenerateLoopBody(reader, list, mapping)
                )
            , br);

            var body = Expression.Block(
                new ParameterExpression[] { list },
                new Expression[] { listInit,
                loop,
                list
                }
                );
            return body;
        }

        /// <summary>
        /// Generate a methdo that takes a data reader and return a list of readed items
        /// </summary>
        /// <param name="readerType">Specific reader type</param>
        /// <param name="mapping">Constructor mapping</param>
        public static Expression<Func<TReader, List<TItem>>> GenerateReaderMethod<TReader, TItem>(CtorMapping mapping)
        {
            var readerType = typeof(TReader);

            if (readerType.IsInterface)
                throw new ArgumentException("Pass an specific reader type");
            var readerArg = Expression.Parameter(readerType, "reader");
            var body = GenerateMethodBody(readerArg, mapping);
            return Expression.Lambda<Func<TReader, List<TItem>>>(body, readerArg);
        }
    }
}
