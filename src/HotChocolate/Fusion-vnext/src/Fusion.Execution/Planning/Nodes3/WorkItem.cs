using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes3;

public sealed record WorkItem(
    WorkItemKind Kind,
    ISyntaxNode Node,
    SelectionSet SelectionSet,
    Lookup? Lookup = null);


