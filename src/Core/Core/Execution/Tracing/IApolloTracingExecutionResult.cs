using System.Collections.Generic;

namespace HotChocolate.Execution.Tracing
{
    internal interface IApolloTracingExecutionResult
    {
        IReadOnlyCollection<IApolloTracingResolverResult> Resolvers { get; }
    }
}
