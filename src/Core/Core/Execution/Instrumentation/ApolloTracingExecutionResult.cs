using System.Collections.Generic;

namespace HotChocolate.Execution.Instrumentation
{
    internal class ApolloTracingExecutionResult
    {
        public IReadOnlyCollection<ApolloTracingResolverResult> Resolvers
        {
            get;
            set;
        }
    }
}
