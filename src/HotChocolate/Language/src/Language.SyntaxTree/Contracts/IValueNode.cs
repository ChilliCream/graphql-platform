namespace HotChocolate.Language;

/// <summary>
/// A GraphQL value literal.
/// </summary>
public interface IValueNode : ISyntaxNode
{
    /// <summary>
    /// Gets the value.
    /// </summary>
    object? Value { get; }
}
