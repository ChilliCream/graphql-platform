using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

public interface IFragment2 : IOptionalSelection2
{
    public int Id { get; }

    IObjectType TypeCondition { get; }

    ISyntaxNode SyntaxNode { get; }

    IReadOnlyList<DirectiveNode> Directives { get; }

    int SelectionSetId { get; }

    ISelectionSet2 SelectionSet { get; }

    string? GetLabel(IVariableValueCollection variables);
}
