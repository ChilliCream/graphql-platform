using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Utilities;

namespace HotChocolate.Types;

/// <summary>
/// Represents an argument value assignment.
/// </summary>
public sealed class ArgumentAssignment : INameProvider, ISyntaxNodeProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentAssignment"/> class.
    /// </summary>
    /// <param name="name">
    /// The name of the argument to which a value is assigned to.
    /// </param>
    /// <param name="value">
    /// The value that is assigned to the argument.
    /// </param>
    public ArgumentAssignment(string name, string value)
        : this(name, new StringValueNode(value))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentAssignment"/> class.
    /// </summary>
    /// <param name="name">
    /// The name of the argument to which a value is assigned to.
    /// </param>
    /// <param name="value">
    /// The value that is assigned to the argument.
    /// </param>
    public ArgumentAssignment(string name, int value)
        : this(name, new IntValueNode(value))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentAssignment"/> class.
    /// </summary>
    /// <param name="name">
    /// The name of the argument to which a value is assigned to.
    /// </param>
    /// <param name="value">
    /// The value that is assigned to the argument.
    /// </param>
    public ArgumentAssignment(string name, double value)
        : this(name, new FloatValueNode(value))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentAssignment"/> class.
    /// </summary>
    /// <param name="name">
    /// The name of the argument to which a value is assigned to.
    /// </param>
    /// <param name="value">
    /// The value that is assigned to the argument.
    /// </param>
    public ArgumentAssignment(string name, bool value)
        : this(name, new BooleanValueNode(value))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentAssignment"/> class.
    /// </summary>
    /// <param name="name">
    /// The name of the argument to which a value is assigned to.
    /// </param>
    /// <param name="value">
    /// The value that is assigned to the argument.
    /// </param>
    public ArgumentAssignment(string name, IValueNode value)
    {
        Name = name.EnsureGraphQLName();
        Value = value;
    }

    /// <summary>
    /// Gets the name of the argument to which a value is assigned to.
    /// </summary>
    /// <value>
    /// The name of the argument.
    /// </value>
    public string Name { get; }

    /// <summary>
    /// Gets the value that is assigned to the argument.
    /// </summary>
    /// <value>
    /// The value of the argument.
    /// </value>
    public IValueNode Value { get; }

    public SchemaCoordinate Coordinate => throw new NotImplementedException();

    /// <summary>
    /// Returns a string representation of the current argument assignment.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
        => ToSyntaxNode().ToString(indented: true);

    /// <summary>
    /// Creates an <see cref="ArgumentNode"/> from an <see cref="ArgumentAssignment"/>.
    /// </summary>
    public ArgumentNode ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => ToSyntaxNode();
}
