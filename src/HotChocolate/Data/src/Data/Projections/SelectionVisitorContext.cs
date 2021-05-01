using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections
{
    public class SelectionVisitorContext : ISelectionVisitorContext
    {
        public SelectionVisitorContext(IResolverContext context)
        {
            Selection = new Stack<ISelection>();
            SelectionSetNodes = new Stack<SelectionSetNode?>();
            ResolvedType = new Stack<INamedType?>();
            Context = context;
        }

        public Stack<ISelection> Selection { get; }

        public Stack<SelectionSetNode?> SelectionSetNodes { get; }

        public Stack<INamedType?> ResolvedType { get; }

        public IResolverContext Context { get; }
    }
}
