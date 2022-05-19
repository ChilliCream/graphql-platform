using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Represents an object field literal.
/// </summary>
public sealed class ObjectFieldNode : ISyntaxNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="ObjectFieldNode"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the field.
    /// </param>
    /// <param name="value">
    /// The assigned field value.
    /// </param>
    public ObjectFieldNode(string name, bool value)
        : this(null, new NameNode(name), new BooleanValueNode(value))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectFieldNode"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the field.
    /// </param>
    /// <param name="value">
    /// The assigned field value.
    /// </param>
    public ObjectFieldNode(string name, int value)
        : this(null, new NameNode(name), new IntValueNode(value))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectFieldNode"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the field.
    /// </param>
    /// <param name="value">
    /// The assigned field value.
    /// </param>
    public ObjectFieldNode(string name, double value)
        : this(null, new NameNode(name), new FloatValueNode(value))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectFieldNode"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the field.
    /// </param>
    /// <param name="value">
    /// The assigned field value.
    /// </param>
    public ObjectFieldNode(string name, string value)
        : this(null, new NameNode(name), new StringValueNode(value))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectFieldNode"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the field.
    /// </param>
    /// <param name="value">
    /// The assigned field value.
    /// </param>
    public ObjectFieldNode(string name, IValueNode value)
        : this(null, new NameNode(name), value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectFieldNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of the field.
    /// </param>
    /// <param name="value">
    /// The assigned field value.
    /// </param>
    public ObjectFieldNode(Location? location, NameNode name, IValueNode value)
    {
        Location = location;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.ObjectField;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <summary>
    /// Gets the name of the field.
    /// </summary>
    public NameNode Name { get; }

    /// <summary>
    /// Gets the assigned field value.
    /// </summary>
    public IValueNode Value { get; }

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Name;
        yield return Value;
    }

    /// <summary>
    /// Determines whether the specified <see cref="ObjectFieldNode"/>
    /// is equal to the current <see cref="ObjectFieldNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="ObjectFieldNode"/> to compare with the current
    /// <see cref="ObjectFieldNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="ObjectFieldNode"/> is equal
    /// to the current <see cref="ObjectFieldNode"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(ObjectFieldNode? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(other, this))
        {
            return true;
        }

        return other.Name.Equals(Name) && other.Value.Equals(Value);
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
    public ObjectFieldNode WithLocation(Location? location) => new(location, Name, Value);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Name" /> with <paramref name="name" />.
    /// </summary>
    /// <param name="name">
    /// The name that shall be used to replace the current <see cref="NamedSyntaxNode.Name" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="name" />.
    /// </returns>
    public ObjectFieldNode WithName(NameNode name) => new(Location, name, Value);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Value" /> with <paramref name="value" />.
    /// </summary>
    /// <param name="value">
    /// The value that shall be used to replace the current <see cref="Value" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="value" />.
    /// </returns>
    public ObjectFieldNode WithValue(IValueNode value) => new(Location, Name, value);
}
