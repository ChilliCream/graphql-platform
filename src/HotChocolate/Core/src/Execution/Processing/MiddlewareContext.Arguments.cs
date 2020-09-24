using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing
{
    internal partial class MiddlewareContext : IMiddlewareContext
    {
        public IReadOnlyDictionary<NameString, ArgumentValue> Arguments { get; set; } =
            default!;

        public T Argument<T>(NameString name)
        {
            if (typeof(IValueNode).IsAssignableFrom(typeof(T)))
            {
                IValueNode literal = ArgumentLiteral<IValueNode>(name);

                if (literal is T casted)
                {
                    return casted;
                }

                throw ResolverContext_LiteralNotCompatible(
                    FieldSelection, Path, name,  typeof(T), literal.GetType());
            }

            return ArgumentValue<T>(name);
        }

        public T ArgumentValue<T>(NameString name)
        {
            if (!Arguments.TryGetValue(name, out ArgumentValue? argument))
            {
                throw ResolverContext_ArgumentDoesNotExist(FieldSelection, Path, name);
            }

            return CoerceArgumentValue<T>(argument);
        }

        public Optional<T> ArgumentOptional<T>(NameString name)
        {
            if (!Arguments.TryGetValue(name, out ArgumentValue? argument))
            {
                throw ResolverContext_ArgumentDoesNotExist(FieldSelection, Path, name);
            }

            return argument.IsImplicit
                ? Optional<T>.Empty(CoerceArgumentValue<T>(argument))
                : new Optional<T>(CoerceArgumentValue<T>(argument));
        }

        public T ArgumentLiteral<T>(NameString name) where T : IValueNode
        {
            if (!Arguments.TryGetValue(name, out ArgumentValue? argument))
            {
                throw ResolverContext_ArgumentDoesNotExist(FieldSelection, Path, name);
            }

            IValueNode literal = argument.ValueLiteral!;

            if (literal is T castedLiteral)
            {
                return castedLiteral;
            }

            throw ResolverContext_LiteralNotCompatible(
                FieldSelection, Path, name,  typeof(T), literal.GetType());
        }

        public ValueKind ArgumentKind(NameString name)
        {
            if (!Arguments.TryGetValue(name, out ArgumentValue? argument))
            {
                throw ResolverContext_ArgumentDoesNotExist(FieldSelection, Path, name);
            }

            // There can only be no kind if there was an error which would have
            // already been raised at this point.
            return argument.Kind ?? ValueKind.Unknown;
        }

        private T CoerceArgumentValue<T>(ArgumentValue argument)
        {
            object? value = argument.Value;

            // if the argument is final and has an already coerced
            // runtime version we can skip over parsing it.
            if (!argument.IsFinal)
            {
                value = argument.Type.ParseLiteral(argument.ValueLiteral!);
                value = argument.Formatter is not null ? argument.Formatter.OnAfterDeserialize(value) : value;
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

            // GraphQL literals are not allowed.
            if (typeof(IValueNode).IsAssignableFrom(typeof(T)))
            {
                throw ResolverContext_LiteralsNotSupported(
                    FieldSelection, Path, argument.Argument.Name, typeof(T));
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

            // we are unable to convert the argument to the request type.
            throw ResolverContext_CannotConvertArgument(
                FieldSelection, Path, argument.Argument.Name, typeof(T));
        }
    }
}
