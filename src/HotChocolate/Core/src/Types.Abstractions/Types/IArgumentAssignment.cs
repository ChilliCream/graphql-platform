using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// Represents an argument value assignment.
/// </summary>
public interface IArgumentAssignment : ISyntaxNodeProvider
{
    /// <summary>
    /// Gets the argument name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the argument value.
    /// </summary>
    IValueNode Value { get; }
}
