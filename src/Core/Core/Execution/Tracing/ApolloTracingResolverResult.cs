using System.Collections.Generic;

namespace HotChocolate.Execution.Tracing
{
    internal sealed class ApolloTracingResolverResult
    {
        public long StartOffset { get; set; }

        public long Duration { get; set; }

        IReadOnlyCollection<object> Path { get;  }

        string ParentType { get; }

        string FieldName { get; }

        string ReturnType { get; }
    }
}
