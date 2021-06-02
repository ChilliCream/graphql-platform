
using System.Collections.Generic;

namespace HotChocolate.Analyzers.Types
{
    public class OperationDirective
    {
        public IReadOnlyList<OperationKind> Operations { get; set; } = default!;
    }
}
