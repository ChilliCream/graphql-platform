using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    public interface IFragment : IOptionalSelection
    {
        public int Id { get; }

        IObjectType TypeCondition { get; }

        ISyntaxNode SyntaxNode { get; }

        IReadOnlyList<DirectiveNode> Directives { get; }

        ISelectionSet SelectionSet { get; }

        string? GetLabel(IVariableValueCollection variables);
    }
}
