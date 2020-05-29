using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation
{
    public interface IDiagnosticEvents
    {
        IActivityScope ParseDocument(IRequestContext context);

        void SyntaxError(IRequestContext context, IError error);

        IActivityScope ValidateDocument(IRequestContext context);

        void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors);

        IActivityScope ResolveFieldValue(IMiddlewareContext context);

        void ResolverError(IMiddlewareContext context, IError error);

        void AddedDocumentToCache(IRequestContext context);

        void RetrievedDocumentFromCache(IRequestContext context);

        void BatchDispatched(IRequestContext context);

        void ExecutorCreated(string name, IRequestExecutor executor);

        void ExecutorEvicted(string name, IRequestExecutor executor);
    }
}
