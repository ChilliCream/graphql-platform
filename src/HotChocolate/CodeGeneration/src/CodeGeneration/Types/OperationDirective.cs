
using System.Collections.Generic;

namespace HotChocolate.CodeGeneration.Types
{
    public class OperationDirective
    {
        public IReadOnlyList<OperationKind> Operations { get; set; } = default!;
    }
}
