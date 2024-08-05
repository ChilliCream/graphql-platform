#nullable enable

using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Language.SyntaxComparison;

namespace HotChocolate.Types;

/// <summary>
/// Represents a directive instance.
/// </summary>
public sealed class Directive
{
    private readonly DirectiveNode _syntaxNode;
    private readonly object _runtimeValue;

    internal Directive(DirectiveType type, DirectiveNode syntaxNode, object runtimeValue)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        _syntaxNode = syntaxNode ?? throw new ArgumentNullException(nameof(syntaxNode));
        _runtimeValue = runtimeValue ?? throw new ArgumentNullException(nameof(runtimeValue));
    }

    /// <summary>
    /// Gets the directive type.
    /// </summary>
    public DirectiveType Type { get; }

    /// <summary>
    /// Gets an argument value of the directive by its  <paramref name="name"/>.
    /// </summary>
    /// <param name="name">
    /// The argument name.
    /// </param>
    /// <typeparam name="T">
    /// The expected type of the argument.
    /// </typeparam>
    /// <returns>
    /// Returns the argument value.
    /// </returns>
    public T GetArgumentValue<T>(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(name);
        }

        return Type.ParseArgument<T>(name, GetArgumentValueOrNull(name));
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

    /// <summary>
    /// Gets the runtime representation of the directive.
    /// </summary>
    /// <typeparam name="T">
    /// The runtime type.
    /// </typeparam>
    /// <returns>
    /// Returns the runtime representation of the directive.
    /// </returns>
    public T AsValue<T>() => (T)_runtimeValue;

    /// <summary>
    /// Gets the syntax node representation of the directive.
    /// </summary>
    /// <param name="removeDefaults"></param>
    /// <returns></returns>
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

            if ((argumentDefinition.DefaultValue is not null &&
                    argumentDefinition.DefaultValue.Equals(argumentValue.Value, Syntax)) ||
                (argumentDefinition.DefaultValue is null &&
                    argumentValue.Value.Kind is SyntaxKind.NullValue))
            {
                if (rewrittenArguments is null)
                {
                    rewrittenArguments ??= new ArgumentNode[_syntaxNode.Arguments.Count];

                    for (var j = 0; j <= i - 1; j++)
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
