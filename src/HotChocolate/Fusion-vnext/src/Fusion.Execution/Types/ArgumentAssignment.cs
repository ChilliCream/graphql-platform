using HotChocolate.Language;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents an argument value assignment.
/// </summary>
public sealed class ArgumentAssignment
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
        Name = name;
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

    /// <summary>
    /// Converts the argument assignment into a syntax node.
    /// </summary>
    /// <returns>
    /// Returns a syntax node that represents the argument assignment.
    /// </returns>
    public ArgumentNode ToSyntaxNode()
        => new(new NameNode(Name), Value);
}
