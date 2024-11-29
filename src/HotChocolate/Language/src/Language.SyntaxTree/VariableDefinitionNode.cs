using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// The variable definition syntax of a GraphQL operation.
/// </summary>
public sealed class VariableDefinitionNode : ISyntaxNode, IHasDirectives
{
    /// <summary>
    /// Initializes a new instance of <see cref="VariableDefinitionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="variable">
    /// The variable.
    /// </param>
    /// <param name="type">
    /// The variable type.
    /// </param>
    /// <param name="defaultValue">
    /// The variables default value.
    /// </param>
    /// <param name="directives">
    /// The directives of this variable declaration.
    /// </param>
    public VariableDefinitionNode(
        Location? location,
        VariableNode variable,
        ITypeNode type,
        IValueNode? defaultValue,
        IReadOnlyList<DirectiveNode> directives)
    {
        Location = location;
        Variable = variable ?? throw new ArgumentNullException(nameof(variable));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        DefaultValue = defaultValue;
        Directives = directives ?? throw new ArgumentNullException(nameof(directives));
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.VariableDefinition;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <summary>
    /// Gets the variable.
    /// </summary>
    public VariableNode Variable { get; }

    /// <summary>
    /// Gets the variable type.
    /// </summary>
    public ITypeNode Type { get; }

    /// <summary>
    /// Gets the variables default value.
    /// </summary>
    public IValueNode? DefaultValue { get; }

    /// <summary>
    /// Gets the directives of this variable declaration.
    /// </summary>
    public IReadOnlyList<DirectiveNode> Directives { get; }

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Variable;
        yield return Type;

        if (DefaultValue is not null)
        {
            yield return DefaultValue;
        }

        foreach (var directive in Directives)
        {
            yield return directive;
        }
    }

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public override string ToString() => SyntaxPrinter.Print(this, true);

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <param name="indented">
    /// A value that indicates whether the GraphQL output should be formatted,
    /// which includes indenting nested GraphQL tokens, adding
    /// new lines, and adding white space between property names and values.
    /// </param>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Location" /> with <paramref name="location" />.
    /// </summary>
    /// <param name="location">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="location" />.
    /// </returns>
    public VariableDefinitionNode WithLocation(Location? location)
        => new(location, Variable, Type, DefaultValue, Directives);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Variable" /> with <paramref name="variable" />.
    /// </summary>
    /// <param name="variable">
    /// The variable that shall be used to replace the current <see cref="Variable" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="variable" />.
    /// </returns>
    public VariableDefinitionNode WithVariable(VariableNode variable)
        => new(Location, variable, Type, DefaultValue, Directives);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Type" /> with <paramref name="type" />.
    /// </summary>
    /// <param name="type">
    /// The type that shall be used to replace the current <see cref="Type" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="type" />.
    /// </returns>
    public VariableDefinitionNode WithType(ITypeNode type)
        => new(Location, Variable, type, DefaultValue, Directives);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="DefaultValue" /> with <paramref name="defaultValue" />.
    /// </summary>
    /// <param name="defaultValue">
    /// The defaultValue that shall be used to replace the current <see cref="DefaultValue" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="defaultValue" />.
    /// </returns>
    public VariableDefinitionNode WithDefaultValue(IValueNode? defaultValue)
        => new(Location, Variable, Type, defaultValue, Directives);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Directives" /> with <paramref name="directives" />.
    /// </summary>
    /// <param name="directives">
    /// The directives that shall be used to replace the current
    /// <see cref="Directives" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="directives" />.
    /// </returns>
    public VariableDefinitionNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Variable, Type, DefaultValue, directives);
}
