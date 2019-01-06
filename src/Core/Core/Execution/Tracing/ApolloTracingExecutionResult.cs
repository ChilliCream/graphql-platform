using System.Collections.Generic;

namespace HotChocolate.Execution.Tracing
{
    internal sealed class ApolloTracingExecutionResult
    {
        public IReadOnlyCollection<ApolloTracingResolverResult> Resolvers
        {
            get;
            set;
        }
    }
}
