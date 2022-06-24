using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyCleanup;

/// <summary>
/// Provides details of the <see cref="ISyntaxNode"/> to be cleaned up.
/// </summary>
public sealed class CleanupInfo
{
    public CleanupInfo(ISyntaxNode node)
    {
        Kind = node.Kind;
        Location = node.Location;
    }

    public SyntaxKind Kind { get; }
    public Language.Location? Location { get; }
}
