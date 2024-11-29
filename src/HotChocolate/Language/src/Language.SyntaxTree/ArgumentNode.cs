using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// This syntax node represents a argument value of a <see cref="FieldNode"/>.
/// </summary>
public sealed class ArgumentNode : ISyntaxNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="ArgumentNode"/>.
    /// </summary>
    /// <param name="name">
    /// The argument name.
    /// </param>
    /// <param name="value">
    /// The argument value.
    /// </param>
    public ArgumentNode(string name, string value)
        : this(null, new NameNode(name), new StringValueNode(value))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ArgumentNode"/>.
    /// </summary>
    /// <param name="name">
    /// The argument name.
    /// </param>
    /// <param name="value">
    /// The argument value.
    /// </param>
    public ArgumentNode(string name, int value)
        : this(null, new NameNode(name), new IntValueNode(value))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ArgumentNode"/>.
    /// </summary>
    /// <param name="name">
    /// The argument name.
    /// </param>
    /// <param name="value">
    /// The argument value.
    /// </param>
    public ArgumentNode(string name, bool value)
        : this(null, new NameNode(name), new BooleanValueNode(value))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ArgumentNode"/>.
    /// </summary>
    /// <param name="name">
    /// The argument name.
    /// </param>
    /// <param name="value">
    /// The argument value.
    /// </param>
    public ArgumentNode(string name, IValueNode value)
        : this(null, new NameNode(name), value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ArgumentNode"/>.
    /// </summary>
    /// <param name="name">
    /// The argument name.
    /// </param>
    /// <param name="value">
    /// The argument value.
    /// </param>
    public ArgumentNode(NameNode name, IValueNode value)
        : this(null, name, value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ArgumentNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The argument name.
    /// </param>
    /// <param name="value">
    /// The argument value.
    /// </param>
    public ArgumentNode(Location? location, NameNode name, IValueNode value)
    {
        Location = location;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.Argument;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <summary>
    /// The name of the argument.
    /// </summary>
    public NameNode Name { get; }

    /// <summary>
    /// The value of the argument.
    /// </summary>
    public IValueNode Value { get; }

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Name;
        yield return Value;
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
    public ArgumentNode WithLocation(Location? location)
        => new(location, Name, Value);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Name" /> with <paramref name="name" />.
    /// </summary>
    /// <param name="name">
    /// The name that shall be used to replace the current name.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="name" />.
    /// </returns>
    public ArgumentNode WithName(NameNode name)
        => new(Location, name, Value);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Value" /> with <paramref name="value" />.
    /// </summary>
    /// <param name="value">
    /// The value that shall be used to replace the current value.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="value" />.
    /// </returns>
    public ArgumentNode WithValue(IValueNode value)
        => new(Location, Name, value);
}
