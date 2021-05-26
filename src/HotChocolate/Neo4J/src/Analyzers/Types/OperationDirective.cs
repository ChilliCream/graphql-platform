
using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class OperationDirective
    {
        public OperationDirective(IReadOnlyList<OperationKind> operations)
        {
            Operations = operations;
        }

        public IReadOnlyList<OperationKind> Operations { get; }
    }
}
