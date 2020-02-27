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
            public PropTypeMapping(string methodName, bool needsNullCheck, SpecialPropTypeMapping special)
            {
                MethodName = methodName;
                NeedsNullCheck = needsNullCheck;
                Special = special;
            }

            public string MethodName { get; }
            public bool NeedsNullCheck { get; }
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
            //If the type is string doesn't need null check, since the GetString() method can return nulls
            var needNullCheck = type != typeof(string) && acceptsNull;

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

            return new PropTypeMapping(methodName, needNullCheck, special);
        }

        static Expression GenerateSingularMapping(Expression reader, SingularMapping mapping)
        {
            var typeMap = GetPropTypeMapping(mapping.Type);
            var readerType = reader.Type;

            Expression indexExpr = Expression.Constant(mapping.ColumnId);
            Expression readExpr = Expression.Call(reader, typeMap.MethodName, new Type[0], indexExpr);
            Expression isDbNullExpr = Expression.Call(reader, nameof(IDataReader.IsDBNull), new Type[0], indexExpr);

            Expression readCondExpr =
                    !typeMap.NeedsNullCheck? readExpr : Expression.Condition(isDbNullExpr, Expression.Constant(null, readExpr.Type), readExpr);
            return readCondExpr;
        }

      

        /// <summary>
        /// Generate an expression for a given mapping
        /// </summary>
        static Expression GenerateMapping(Expression reader, ValueMapping mapping)
        {
            switch (mapping)
            {
                case SingularMapping singular:
                    return GenerateSingularMapping(reader, singular);
                case CtorMapping ctor:
                    return GenerateCtorMapping(reader, ctor);
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Generate an expression for a constructor mapping
        /// </summary>
        static Expression GenerateCtorMapping(Expression reader, CtorMapping mapping)
        {
            var parsMapping = mapping.ConstructorColumnMapping.Select(x => GenerateMapping(reader, x));
            var ctorCall = Expression.New(mapping.Constructor, parsMapping);

            var propsSet = mapping.PropertyMapping.Select(x => Expression.Bind(x.Key, GenerateMapping(reader, x.Value)));
            var init = Expression.MemberInit(ctorCall, propsSet);
            return init;
        }

        /// <summary>
        /// Generate the constructor call for creating the item instance
        /// </summary>
        internal static Expression GenerateConsCall(Expression reader, CtorMapping mapping)
        {
            return Expression.New(mapping.Constructor);

        }

        public static Expression GenerateLoopBody(Expression reader, Expression list, ValueMapping mapping)
        {
            var itemType = (mapping is ValueMapping typed ? typed.Type : typeof(object));
            var body = new List<Expression>();

            var itemVar = Expression.Variable(itemType, "item");
            body.Add(Expression.Assign(itemVar, GenerateMapping(reader, mapping)));
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
        static Expression GenerateMethodBody(Expression reader, ValueMapping mapping)
        {
            var br = Expression.Label("break");
            var itemType = (mapping is ValueMapping typed ? typed.Type : typeof(object));
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
