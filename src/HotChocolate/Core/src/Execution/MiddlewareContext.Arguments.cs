using System.Collections.Generic;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal partial class MiddlewareContext : IMiddlewareContext
    {
        public IReadOnlyDictionary<NameString, PreparedArgument> Arguments { get; set; } =
            default!;

        public T Argument<T>(NameString name)
        {
            if (typeof(IValueNode).IsAssignableFrom(typeof(T)))
            {
                if (ArgumentLiteral<IValueNode>(name) is T casted)
                {
                    return casted;
                }

                // todo: not compatible literal
                throw new GraphQLException(); // throw helper
            }

            return ArgumentValue<T>(name);
        }

        public T ArgumentValue<T>(NameString name)
        {
            if (!Arguments.TryGetValue(name, out PreparedArgument? argument))
            {
                throw new GraphQLException(); // throw helper
            }

            return CoerceArgumentValue<T>(argument);
        }

        public Optional<T> ArgumentOptional<T>(NameString name)
        {
            if (!Arguments.TryGetValue(name, out PreparedArgument? argument))
            {
                throw new GraphQLException(); // throw helper
            }

            return new Optional<T>(CoerceArgumentValue<T>(argument), !argument.IsImplicit);
        }

        public T ArgumentLiteral<T>(NameString name) where T : IValueNode
        {
            if (!Arguments.TryGetValue(name, out PreparedArgument? argument))
            {
                throw new GraphQLException(); // throw helper
            }

            IValueNode literal = argument.ValueLiteral!;

            if (literal is T castedLiteral)
            {
                return castedLiteral;
            }

            // not compatible literal
            throw new GraphQLException(); // throw helper
        }

        public ValueKind ArgumentKind(NameString name)
        {
            if (!Arguments.TryGetValue(name, out PreparedArgument? argument))
            {
                throw new GraphQLException(); // throw helper
            }

            // There can only be no kind if there was an error which would have
            // already been raised at this point.
            return argument.Kind ?? ValueKind.Unknown;
        }

        private T CoerceArgumentValue<T>(PreparedArgument argument)
        {
            object? value = argument.Value;

            if (!argument.IsFinal)
            {
                value = argument.Type.ParseLiteral(argument.ValueLiteral!);
            }

            if (value is null)
            {
                // if there was a non-null violation the exception would already been triggered.
                return default!;
            }

            if (value is T castedValue ||
                _operationContext.Converter.TryConvert<object, T>(value, out castedValue))
            {
                return castedValue;
            }

            // If the object is internally held as a dictionary structure we will try to
            // create from this the required argument value.
            // This however comes with a performance impact of traversing the dictionary structure
            // and creating from this the object.
            if (value is IReadOnlyDictionary<string, object> || value is IReadOnlyList<object>)
            {
                var dictToObjConverter = new DictionaryToObjectConverter(_operationContext.Converter);
                if (typeof(T).IsInterface)
                {
                    object o = dictToObjConverter.Convert(value, argument.Type.RuntimeType);
                    if (o is T c)
                    {
                        return c;
                    }
                }
                else
                {
                    return (T)dictToObjConverter.Convert(value, typeof(T));
                }
            }

            // not compatible literal
            throw new GraphQLException(); // throw helper
        }
    }
}
