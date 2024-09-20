using HotChocolate.Language.Properties;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>Represents a enum value literal.</para>
/// <para>http://facebook.github.io/graphql/June2018/#sec-Enum-Value</para>
/// </summary>
public sealed class EnumValueNode : IValueNode<string>
{
    /// <summary>
    /// Initializes a new instance of <see cref="EnumTypeDefinitionNode"/>.
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public EnumValueNode(object value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var stringValue = value.ToString()?.ToUpperInvariant();

        Value = stringValue ??
            throw new ArgumentException(
                Resources.EnumValueNode_ValueIsNull,
                nameof(value));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EnumTypeDefinitionNode"/>.
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public EnumValueNode(string value)
        : this(null, value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EnumTypeDefinitionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public EnumValueNode(Location? location, string value)
    {
        Location = location;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.EnumValue;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <inheritdoc cref="IValueNode{T}" />
    public string Value { get; }

    /// <inheritdoc cref="IValueNode" />
    object IValueNode.Value => Value;

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes() => [];

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
    public EnumValueNode WithLocation(Location? location)
        => new(location, Value);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Value" /> with <paramref name="value" />.
    /// </summary>
    /// <param name="value">
    /// The value of this literal.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="value" />.
    /// </returns>
    public EnumValueNode WithValue(string value)
        => new(Location, value);
}
