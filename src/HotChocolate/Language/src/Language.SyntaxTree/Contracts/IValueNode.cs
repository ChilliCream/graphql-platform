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
    IValueNode WithLocation(Location? location);
}
