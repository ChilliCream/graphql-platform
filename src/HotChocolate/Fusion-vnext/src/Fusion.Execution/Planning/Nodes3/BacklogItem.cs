using System.ComponentModel.Design;
using HotChocolate.Fusion.Planning.Nodes;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.Planning.Nodes3;

public sealed record BacklogItem(
    PlanNodeKind Kind,
    ISyntaxNode Node,
    SelectionSet SelectionSet);


