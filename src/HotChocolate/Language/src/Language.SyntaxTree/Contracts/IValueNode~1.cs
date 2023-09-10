namespace HotChocolate.Language;

/// <summary>
/// A GraphQL value literal.
/// </summary>
public interface IValueNode<out T> : IValueNode
{
    /// <summary>
    /// Gets the value.
    /// </summary>
    new T Value { get; }
}
