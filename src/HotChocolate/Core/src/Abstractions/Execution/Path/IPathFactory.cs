namespace HotChocolate.Execution;

/// <summary>
/// A factory for the different kinds of <see cref="Path"/>
/// </summary>
public interface IPathFactory
{
    /// <summary>
    /// Appends an element.
    /// </summary>
    /// <param name="parent">The parent</param>
    /// <param name="index">The index of the element.</param>
    /// <returns>Returns a new path segment pointing to an element in a list.</returns>
    IndexerPathSegment Append(Path parent, int index);

    /// <summary>
    /// Appends a new path segment.
    /// </summary>
    /// <param name="parent">The parent</param>
    /// <param name="name">The name of the path segment.</param>
    /// <returns>Returns a new path segment.</returns>
    NamePathSegment Append(Path parent, NameString name);
}
