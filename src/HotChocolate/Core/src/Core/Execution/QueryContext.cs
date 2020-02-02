using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    public class QueryContext
        : IQueryContext
    {
        private Func<ObjectField, FieldNode, FieldDelegate> _middlewareResolver;
        private IReadOnlyQueryRequest _request;
        private string _queryKey;

        public QueryContext(
            ISchema schema,
            IRequestServiceScope serviceScope,
            IReadOnlyQueryRequest request,
            Func<ObjectField, FieldNode, FieldDelegate> middlewareResolver,
            CancellationToken requestAborted)
            : this(schema, serviceScope, request, middlewareResolver)
        {
            RequestAborted = requestAborted;
        }

        public QueryContext(
            ISchema schema,
            IRequestServiceScope serviceScope,
            IReadOnlyQueryRequest request,
            Func<ObjectField, FieldNode, FieldDelegate> middlewareResolver)
        {
            Schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            Request = request
                ?? throw new ArgumentNullException(nameof(request));
            ServiceScope = serviceScope
                ?? throw new ArgumentNullException(nameof(serviceScope));
            MiddlewareResolver = middlewareResolver
                ?? throw new ArgumentNullException(nameof(middlewareResolver));

            ContextData = request.Properties == null
                ? new ConcurrentDictionary<string, object>()
                : new ConcurrentDictionary<string, object>(request.Properties);
        }

        public ISchema Schema { get; }

        public IReadOnlyQueryRequest Request
        {
            get => _request;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(
                        nameof(value),
                        "The request mustn't be null.");
                }
                _request = value;
            }
        }

        public string QueryKey
        {
            get => _queryKey;
            set
            {
                if (_queryKey != null)
                {
                    throw new ArgumentException(
                        "The query key can only be set once.",
                        nameof(value));
                }
                _queryKey = value;
            }
        }

        public IRequestServiceScope ServiceScope { get; }

        public IServiceProvider Services => ServiceScope.ServiceProvider;

        public IDictionary<string, object> ContextData { get; }

        public DocumentNode Document { get; set; }

        public ICachedQuery CachedQuery { get; set; }

        public IOperation Operation { get; set; }

        public QueryValidationResult ValidationResult { get; set; }

        public CancellationToken RequestAborted { get; set; }

        public IExecutionResult Result { get; set; }

        public Exception Exception { get; set; }

        public Func<ObjectField, FieldNode, FieldDelegate> MiddlewareResolver
        {
            get => _middlewareResolver;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(
                        nameof(value),
                        "The middleware resolver mustn't be null.");
                }
                _middlewareResolver = value;
            }
        }
    }
}
