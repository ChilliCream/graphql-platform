using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.Properties.Resources;
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

        if (value is IOptional optional)
        {
            return (T)optional.Value!;
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

    public IReadOnlyDictionary<string, ArgumentValue> ReplaceArguments(
        ReplaceArguments replace)
    {
        if (replace is null)
        {
            throw new ArgumentNullException(nameof(replace));
        }

        var original = Arguments;
        Arguments = replace(original) ??
            throw new InvalidOperationException(
                MiddlewareContext_ReplaceArguments_NullNotAllowed);
        return original;
    }

    public ArgumentValue ReplaceArgument(string argumentName, ArgumentValue newArgumentValue)
    {
        if (string.IsNullOrEmpty(argumentName))
        {
            throw new ArgumentNullException(nameof(argumentName));
        }

        if (newArgumentValue is null)
        {
            throw new ArgumentNullException(nameof(newArgumentValue));
        }

        Dictionary<string, ArgumentValue> mutableArguments;

        // if the arguments is a dictionary instance we will take it a mutable and will
        // replace in-place.
        if (Arguments is Dictionary<string, ArgumentValue> casted)
        {
            mutableArguments = casted;
        }

        // if we have no mutable argument map we will create a new dictionary and
        // copy the argument state.
        else
        {
            mutableArguments = new Dictionary<string, ArgumentValue>(Arguments);
            Arguments = mutableArguments;
        }

        if (!mutableArguments.TryGetValue(argumentName, out var argumentValue))
        {
            throw new ArgumentException(
                string.Format(MiddlewareContext_ReplaceArgument_InvalidKey, argumentName),
                nameof(argumentName));
        }

        // we remove the original argument name ...
        mutableArguments.Remove(argumentName);

        // and allow the argument to be replaces with a new argument that could also have
        // a new name.
        mutableArguments.Add(newArgumentValue.Name, newArgumentValue);

        // we return the old argument so that a middleware is able to restore the argument
        // state at some point.
        return argumentValue;
    }
}
