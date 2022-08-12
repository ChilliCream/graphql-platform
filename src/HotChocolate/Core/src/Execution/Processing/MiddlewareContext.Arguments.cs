using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal partial class MiddlewareContext
{
    public IReadOnlyDictionary<string, ArgumentValue> Arguments { get; set; } = default!;

    public T ArgumentValue<T>(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (!Arguments.TryGetValue(name, out var argument))
        {
            throw ResolverContext_ArgumentDoesNotExist(_selection.SyntaxNode, Path, name);
        }

        return CoerceArgumentValue<T>(argument);
    }

    public Optional<T> ArgumentOptional<T>(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (!Arguments.TryGetValue(name, out var argument))
        {
            throw ResolverContext_ArgumentDoesNotExist(_selection.SyntaxNode, Path, name);
        }

        return argument.IsDefaultValue
            ? Optional<T>.Empty(CoerceArgumentValue<T>(argument))
            : new Optional<T>(CoerceArgumentValue<T>(argument));
    }

    public TValueNode ArgumentLiteral<TValueNode>(string name) where TValueNode : IValueNode
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (!Arguments.TryGetValue(name, out var argument))
        {
            throw ResolverContext_ArgumentDoesNotExist(_selection.SyntaxNode, Path, name);
        }

        var literal = argument.ValueLiteral!;

        if (literal is TValueNode castedLiteral)
        {
            return castedLiteral;
        }

        throw ResolverContext_LiteralNotCompatible(
            _selection.SyntaxNode, Path, name, typeof(TValueNode), literal.GetType());
    }

    public ValueKind ArgumentKind(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (!Arguments.TryGetValue(name, out var argument))
        {
            throw ResolverContext_ArgumentDoesNotExist(_selection.SyntaxNode, Path, name);
        }

        // There can only be no kind if there was an error which would have
        // already been raised at this point.
        return argument.Kind ?? ValueKind.Unknown;
    }

    private T CoerceArgumentValue<T>(ArgumentValue argument)
    {
        var value = argument.Value;

        // if the argument is final and has an already coerced
        // runtime version we can skip over parsing it.
        if (!argument.IsFullyCoerced)
        {
            value = _parser.ParseLiteral(argument.ValueLiteral!, argument, typeof(T));
        }

        if (value is null)
        {
            // if there was a non-null violation the exception would already been triggered.
            return default!;
        }

        if (value is T castedValue ||
            _operationContext.Converter.TryConvert(value, out castedValue))
        {
            return castedValue;
        }

        // GraphQL literals are not allowed.
        if (typeof(IValueNode).IsAssignableFrom(typeof(T)))
        {
            throw ResolverContext_LiteralsNotSupported(
                _selection.SyntaxNode,
                Path,
                argument.Name,
                typeof(T));
        }

        // we are unable to convert the argument to the request type.
        throw ResolverContext_CannotConvertArgument(
            _selection.SyntaxNode,
            Path,
            argument.Name,
            typeof(T));
    }

    public IReadOnlyDictionary<string, ArgumentValue> ReplaceArguments(
        IReadOnlyDictionary<string, ArgumentValue> argumentValues)
    {
        if (argumentValues is null)
        {
            throw new ArgumentNullException(nameof(argumentValues));
        }

        var original = Arguments;
        Arguments = argumentValues;
        return original;
    }
}
