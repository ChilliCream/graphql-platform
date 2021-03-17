using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections
{
    public class SelectionVisitorContext : ISelectionVisitorContext
    {
        public SelectionVisitorContext(IResolverContext context)
        {
            Selection = new Stack<ISelection>();
            SelectionSetNodes = new Stack<SelectionSetNode?>();
            Context = context;
        }

        public Stack<ISelection> Selection { get; }

        public Stack<SelectionSetNode?> SelectionSetNodes { get; }

        public IResolverContext Context { get; }
    }
}
