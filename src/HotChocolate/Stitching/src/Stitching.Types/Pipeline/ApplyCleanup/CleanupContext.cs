using System;
using System.Collections.Generic;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyCleanup;

internal sealed class CleanupContext : INavigatorContext
{
    public CleanupContext(string sourceName)
    {
        SourceName = sourceName;
    }

    public string SourceName { get; }

    public Dictionary<string, CleanupInfo> Types { get; }
        = new(StringComparer.Ordinal);

    public ISyntaxNavigator Navigator { get; }
        = new DefaultSyntaxNavigator();
}
