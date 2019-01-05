using System.Collections.Generic;

namespace HotChocolate.Execution.Tracing
{
    internal interface IApolloTracingResolverResult
        : IApolloTracingRelativeDurationResult
    {
        IReadOnlyCollection<object> Path { get;  }

        string ParentType { get; }

        string FieldName { get; }

        string ReturnType { get; }
    }
}
