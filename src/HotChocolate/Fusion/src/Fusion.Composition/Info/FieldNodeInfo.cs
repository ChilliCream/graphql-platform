using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Info;

internal record FieldNodeInfo(
    FieldNode FieldNode,
    ImmutableArray<string> FieldNamePath);
