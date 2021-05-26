
using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class OperationDirective
    {
        public IReadOnlyList<OperationKind> Operations { get; set; } = default!;
    }
}
