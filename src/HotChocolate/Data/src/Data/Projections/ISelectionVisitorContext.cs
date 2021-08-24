using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections
{
    public interface ISelectionVisitorContext
    {
        Stack<ISelection> Selection { get; }

        Stack<SelectionSetNode?> SelectionSetNodes { get; }

        Stack<INamedType?> ResolvedType { get; }

        IResolverContext Context { get; }
    }
}
