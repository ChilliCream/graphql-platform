using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public interface ISelection : IOperationNode
{
    SelectionSet? SelectionSet { get; }

    DirectiveCollection Directives { get; }

    new ISelectionNode ToSyntaxNode();
}
