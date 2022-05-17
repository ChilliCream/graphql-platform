namespace HotChocolate.Execution;

/// <summary>
/// A implementation of <see cref="IPathFactory"/> that creates the elements
/// in memory and does not pool the elements.
/// </summary>
public sealed class MemoryPathFactory : BasePathFactory
{
    private MemoryPathFactory() { }

    /// <inheritdoc />
    protected override IndexerPathSegment CreateIndexer() => new();

    /// <inheritdoc />
    protected override NamePathSegment CreateNamed() => new();

    /// <summary>
    /// The default instance of the path factory
    /// </summary>
    public static MemoryPathFactory Instance { get; } = new();
}
