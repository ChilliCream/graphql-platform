using System.Collections.Generic;

namespace HotChocolate.Execution.Instrumentation
{
    internal class ApolloTracingResolverResult
        : ApolloTracingOperationResult
    {
        public IReadOnlyCollection<object> Path { get; set; }

        public string ParentType { get; set; }

        public string FieldName { get; set; }

        public string ReturnType { get; set; }
    }
}
