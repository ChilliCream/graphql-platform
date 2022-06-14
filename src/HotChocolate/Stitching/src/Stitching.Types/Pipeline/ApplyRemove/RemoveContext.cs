using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyRemove;

internal sealed class RemoveContext : INavigatorContext
{
    public RemoveContext(string sourceName)
    {
        SourceName = sourceName;
    }

    public string SourceName { get; }

    public Dictionary<string, RemoveInfo> RemovedTypes { get; } =
        new(StringComparer.Ordinal);

    public Dictionary<string, HashSet<string>> ImplementedBy { get; } =
        new(StringComparer.Ordinal);

    public Dictionary<SchemaCoordinateNode, RemoveInfo> RemovedFields { get; } =
        new(SyntaxComparer.BySyntax);

    public HashSet<string> TypesWithRemovedFields { get; } =
        new HashSet<string>(StringComparer.Ordinal);

    public ISyntaxNavigator Navigator { get; } = new DefaultSyntaxNavigator();
}
