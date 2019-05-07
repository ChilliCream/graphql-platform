using System.Collections.Generic;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public interface IRequestContext
    {
        IRequestServiceScope ServiceScope { get; }

        FieldDelegate ResolveMiddleware(ObjectField field, FieldNode selection);

        ICachedQuery CachedQuery { get; }

        IDictionary<string, object> ContextData { get; }

        QueryExecutionDiagnostics Diagnostics { get; }

        IRequestContext Clone();
    }
}
