namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.A;

/// <summary>
/// Marker interface for the <c>Media</c> type hierarchy in the
/// <c>a</c> subgraph. Implemented by <c>Video</c> and <c>Article</c>.
/// </summary>
public interface IMedia
{
    string Id { get; }

    string? Title { get; }
}
