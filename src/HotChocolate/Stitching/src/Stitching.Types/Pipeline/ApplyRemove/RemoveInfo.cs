using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyRemove;

/// <summary>
/// Provides details of the captured remove directive.
/// </summary>
public sealed class RemoveInfo
{
    public RemoveInfo(DirectiveNode removeDirective)
    {
        RemoveDirective = removeDirective;
    }

    /// <summary>
    /// The directive syntax node to be removed.
    /// </summary>
    public DirectiveNode RemoveDirective { get; }
}
