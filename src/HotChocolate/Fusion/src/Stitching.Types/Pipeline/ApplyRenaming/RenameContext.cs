using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyRenaming;

internal sealed class RenameContext : INavigatorContext
{
    public RenameContext(string sourceName)
    {
        SourceName = sourceName;
    }

    public string SourceName { get; }

    public Dictionary<string, RenameInfo> RenamedTypes { get; } =
        new(StringComparer.Ordinal);

    public Dictionary<string, HashSet<string>> ImplementedBy { get; } =
        new(StringComparer.Ordinal);

    public Dictionary<SchemaCoordinateNode, RenameInfo> RenamedFields { get; } =
        new(SyntaxComparer.BySyntax);

    public HashSet<string> TypesWithFieldRenames { get; } =
        new HashSet<string>(StringComparer.Ordinal);

    public ISyntaxNavigator Navigator { get; } = new DefaultSyntaxNavigator();
}
