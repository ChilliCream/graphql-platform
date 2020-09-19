using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    public interface IFragment
        : IOptionalSelection
    {
        ISyntaxNode SyntaxNode { get; }

        INamedOutputType TypeCondition { get;  }

        ISelectionVariants SelectionVariants { get; }

        IDirectiveCollection Directives { get; }
    }
}
