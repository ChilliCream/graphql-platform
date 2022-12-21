#nullable enable

using System;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Language.SyntaxComparison;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types;

/// <summary>
/// Represents a directive instance.
/// </summary>
public sealed class Directive
{
    private readonly DirectiveNode _syntaxNode;
    private object? _runtimeValue;

    // ReSharper disable once IntroduceOptionalParameters.Global
    public Directive(DirectiveType type, DirectiveNode syntaxNode)
        : this(type, syntaxNode, null)
    {
    }

    public Directive(DirectiveType type, DirectiveNode syntaxNode, object? runtimeValue)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        _syntaxNode = syntaxNode ?? throw new ArgumentNullException(nameof(syntaxNode));
        _runtimeValue = runtimeValue;
    }

    /// <summary>
    /// Gets the directive type.
    /// </summary>
    public DirectiveType Type { get; }

    /// <summary>
    /// Gets an argument value of the directive by its  <paramref name="argumentName"/>.
    /// </summary>
    /// <param name="argumentName">
    /// The argument name.
    /// </param>
    /// <typeparam name="T">
    /// The expected type of the argument.
    /// </typeparam>
    /// <returns>
    /// Returns the argument value.
    /// </returns>
    public T GetArgumentValue<T>(string argumentName)
    {
        if (argumentName is null)
        {
            throw new ArgumentNullException(argumentName);
        }

        if (!Type.Arguments.TryGetField(argumentName, out var argument))
        {
            throw new ArgumentException(
                string.Format(
                    Directive_GetArgumentValue_UnknownArgument,
                    Type.Name,
                    argumentName));
        }

        var value = GetArgumentValueOrNull(argumentName);

        if (value is null)
        {
            value = argument.DefaultValue ?? NullValueNode.Default;
        }

        return Type.ParseArgumentValue<T>(argument, value);
    }

    private IValueNode? GetArgumentValueOrNull(string argumentValue)
    {
        var arguments = _syntaxNode.Arguments;

        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];

            if (argument.Name.Value.EqualsOrdinal(argumentValue))
            {
                return argument.Value;
            }
        }

        return null;
    }

    public T AsValue<T>()
        => (T)(_runtimeValue ??= Type.Parse<T>(_syntaxNode))!;

    public DirectiveNode AsSyntaxNode(bool removeDefaults = false)
    {
        if (!removeDefaults || _syntaxNode.Arguments.Count == 0)
        {
            return _syntaxNode;
        }

        var arguments = _syntaxNode.Arguments;
        ArgumentNode[]? rewrittenArguments = null;
        var index = 0;

        for (var i = 0; i < arguments.Count; i++)
        {
            var argumentValue = arguments[i];
            var argumentDefinition = Type.Arguments[argumentValue.Name.Value];

            if (argumentDefinition.DefaultValue is not null &&
                argumentDefinition.DefaultValue.Equals(argumentValue.Value, Syntax))
            {
                if (rewrittenArguments is null)
                {
                    rewrittenArguments ??= new ArgumentNode[_syntaxNode.Arguments.Count];

                    for (var j = 0; j < i - 1; j++)
                    {
                        rewrittenArguments[index++] = arguments[j];
                    }
                }
            }
            else if (rewrittenArguments is not null)
            {
                rewrittenArguments[index++] = arguments[i];
            }
        }

        if (rewrittenArguments is not null)
        {
            Array.Resize(ref rewrittenArguments, index);
            return _syntaxNode.WithArguments(rewrittenArguments);
        }

        return _syntaxNode;
    }

    /// <summary>
    /// Implicitly casts <see cref="Directive"/> to <see cref="DirectiveNode"/>.
    /// </summary>
    /// <param name="directive">
    /// The directive that shall be casted.
    /// </param>
    /// <returns>
    /// Returns the directive syntax node.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="directive"/> is <c>null</c>.
    /// </exception>
    public static implicit operator DirectiveNode(Directive directive)
    {
        if (directive is null)
        {
            throw new ArgumentNullException(nameof(directive));
        }

        return directive.AsSyntaxNode();
    }
}
