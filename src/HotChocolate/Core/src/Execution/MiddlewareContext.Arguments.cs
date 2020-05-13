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

                // not compatible literal
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

            // not compatible literal
            throw new GraphQLException(); // throw helper
        }
    }
}
