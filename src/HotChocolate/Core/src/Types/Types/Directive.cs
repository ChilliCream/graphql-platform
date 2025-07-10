#nullable enable

using System.Collections.Immutable;
using HotChocolate.Language;
using static HotChocolate.Language.SyntaxComparison;

namespace HotChocolate.Types;

/// <summary>
/// Represents a directive instance.
/// </summary>
public sealed class Directive : IDirective
{
    private readonly DirectiveNode _syntaxNode;
    private readonly object _runtimeValue;

    internal Directive(DirectiveType type, DirectiveNode syntaxNode, object runtimeValue)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        _syntaxNode = syntaxNode ?? throw new ArgumentNullException(nameof(syntaxNode));
        _runtimeValue = runtimeValue ?? throw new ArgumentNullException(nameof(runtimeValue));
        var arguments = syntaxNode.Arguments.Count > 0
            ? syntaxNode.Arguments.Select(a => new ArgumentAssignment(a.Name.Value, a.Value)).ToImmutableArray()
            : [];
        Arguments = new ArgumentAssignmentCollection(arguments);
    }

    /// <summary>
    /// Gets the directive type.
    /// </summary>
    public DirectiveType Type { get; }

    IDirectiveDefinition IDirective.Definition => Type;

    /// <summary>
    /// Gets the name of the directive.
    /// </summary>
    public string Name => Type.Name;

    /// <summary>
    /// Gets the arguments of the directive.
    /// </summary>
    public ArgumentAssignmentCollection Arguments { get; }

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
        ArgumentNullException.ThrowIfNull(name);

        Arguments.TryGetValue(name, out var argumentValue);
        return Type.ParseArgument<T>(name, argumentValue);
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
    public T ToValue<T>() where T : notnull
        => (T)_runtimeValue;

    /// <summary>
    /// Gets the runtime representation of the directive.
    /// </summary>
    [Obsolete("Use ToSyntaxNode(removeDefaults) instead.")]
    public DirectiveNode AsSyntaxNode(bool removeDefaults)
        => ToSyntaxNode(removeDefaults);

    /// <summary>
    /// Returns a string representation of the directive.
    /// </summary>
    public override string ToString() => ToSyntaxNode(removeDefaults: true).ToString();

    /// <summary>
    /// Creates a <see cref="DirectiveNode"/> from a type system member.
    /// </summary>
    public DirectiveNode ToSyntaxNode() => ToSyntaxNode(removeDefaults: true);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => ToSyntaxNode(removeDefaults: true);

    /// <summary>
    /// Gets the syntax node representation of the directive.
    /// </summary>
    /// <param name="removeDefaults">
    /// Remove default values from the directive arguments.
    /// </param>
    public DirectiveNode ToSyntaxNode(bool removeDefaults)
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

            if ((argumentDefinition.DefaultValue?.Equals(argumentValue.Value, Syntax) == true)
                || (argumentDefinition.DefaultValue is null && argumentValue.Value.Kind is SyntaxKind.NullValue))
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
    /// The directive that shall be cast.
    /// </param>
    /// <returns>
    /// Returns the directive syntax node.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="directive"/> is <c>null</c>.
    /// </exception>
    public static implicit operator DirectiveNode(Directive directive)
    {
        ArgumentNullException.ThrowIfNull(directive);
        return directive.ToSyntaxNode();
    }
}
