using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyRenaming;

internal sealed class RewriteContext : INavigatorContext
{
    public RewriteContext(string sourceName)
    {
        SourceName = sourceName;
    }

    public string SourceName { get; }

    public Dictionary<string, RenameInfo> RenamedTypes { get; } = new();

    public Dictionary<string, RenameInfo> RenamedInterfaces { get; } = new();

    public ISyntaxNavigator Navigator { get; } = new DefaultSyntaxNavigator();
}
