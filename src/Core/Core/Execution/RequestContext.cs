using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution
{
    internal class RequestContext
        : IRequestContext
    {
        private readonly Func<FieldSelection, FieldDelegate> _resolveMiddleware;

        public RequestContext(
            IRequestServiceScope serviceScope,
            Func<FieldSelection, FieldDelegate> middlewareResolver,
            IDictionary<string, object> contextData,
            QueryExecutionDiagnostics diagnostics)
        {
            ServiceScope = serviceScope
                ?? throw new ArgumentNullException(nameof(serviceScope));
            _resolveMiddleware = middlewareResolver
                ?? throw new ArgumentNullException(nameof(middlewareResolver));
            ContextData = contextData
                ?? throw new ArgumentNullException(nameof(contextData));
            Diagnostics = diagnostics
                ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public IRequestServiceScope ServiceScope { get; }

        public IDictionary<string, object> ContextData { get; private set; }

        public QueryExecutionDiagnostics Diagnostics { get; }

        public FieldDelegate ResolveMiddleware(FieldSelection fieldSelection)
        {
            return _resolveMiddleware(fieldSelection);
        }

        public IRequestContext Clone()
        {
            return new RequestContext(
                ServiceScope,
                _resolveMiddleware,
                new ConcurrentDictionary<string, object>(ContextData),
                Diagnostics);
        }
    }
}
