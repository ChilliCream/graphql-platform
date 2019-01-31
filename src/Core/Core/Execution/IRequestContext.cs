using System.Collections.Generic;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution
{
    public interface IRequestContext
    {
        IRequestServiceScope ServiceScope { get; }
        FieldDelegate ResolveMiddleware(FieldSelection fieldSelection);
        IDictionary<string, object> ContextData { get; }
        QueryExecutionDiagnostics Diagnostics { get; }
        IRequestContext Clone();
    }
}
