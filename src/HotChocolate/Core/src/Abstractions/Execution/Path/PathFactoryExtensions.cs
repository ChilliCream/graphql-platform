namespace HotChocolate.Execution;

public static class PathFactoryExtensions
{
    /// <summary>
    /// Creates a new <see cref="Path"/> based on the <paramref name="name"/>
    /// </summary>
    /// <param name="factory">The factory</param>
    /// <param name="name">The name of the root element</param>
    public static NamePathSegment New(
        this PathFactory factory,
        NameString name) => factory.Append(Path.Root, name);
}
