using System.Collections.Generic;

namespace HotChocolate.Execution.Instrumentation
{
    internal class ExecutionResult
    {
        public IReadOnlyCollection<ResolverResult> Resolvers
        {
            get;
            set;
        }
    }
}
