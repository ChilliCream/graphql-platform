using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyRenaming;

internal sealed class RewriteContext : ISyntaxVisitorContext
{
    public RewriteContext(string sourceName)
    {
        SourceName = sourceName;
    }

    public string SourceName { get; }
}
