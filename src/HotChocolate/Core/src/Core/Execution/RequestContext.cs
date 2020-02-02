using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    internal class RequestContext
        : IRequestContext
    {
        private readonly Func<ObjectField, FieldNode, FieldDelegate> _factory;

        public RequestContext(
            IRequestServiceScope serviceScope,
            Func<ObjectField, FieldNode, FieldDelegate> middlewareFactory,
            ICachedQuery cachedQuery,
            IDictionary<string, object> contextData,
            QueryExecutionDiagnostics diagnostics)
        {
            ServiceScope = serviceScope
                ?? throw new ArgumentNullException(nameof(serviceScope));
            _factory = middlewareFactory
                ?? throw new ArgumentNullException(nameof(middlewareFactory));
            CachedQuery = cachedQuery
                ?? throw new ArgumentNullException(nameof(cachedQuery));
            ContextData = contextData
                ?? throw new ArgumentNullException(nameof(contextData));
            Diagnostics = diagnostics
                ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public IRequestServiceScope ServiceScope { get; }

        public IDictionary<string, object> ContextData { get; }

        public QueryExecutionDiagnostics Diagnostics { get; }

        public ICachedQuery CachedQuery { get; }

        public FieldDelegate ResolveMiddleware(
            ObjectField field,
            FieldNode selection)
        {
            return _factory(field, selection);
        }

        public IRequestContext Clone()
        {
            IServiceScope serviceScope = ServiceScope.ServiceProvider.CreateScope();

            return new RequestContext(
                new RequestServiceScope(serviceScope.ServiceProvider, serviceScope),
                _factory,
                CachedQuery,
                new ConcurrentDictionary<string, object>(ContextData),
                Diagnostics);
        }
    }
}
