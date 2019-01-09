using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    public class QueryContext
        : IQueryContext
    {
        public QueryContext(
            ISchema schema,
            IRequestServiceScope serviceScope,
            IReadOnlyQueryRequest request)
        {
            Schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            Request = request
                ?? throw new ArgumentNullException(nameof(request));
            ServiceScope = serviceScope
                ?? throw new ArgumentNullException(nameof(serviceScope));


            ContextData = request.Properties == null
                ? new ConcurrentDictionary<string, object>()
                : new ConcurrentDictionary<string, object>(request.Properties);
        }

        public ISchema Schema { get; }
        public IReadOnlyQueryRequest Request { get; }
        public IRequestServiceScope ServiceScope { get; }
        public IServiceProvider Services => ServiceScope.ServiceProvider;
        public IDictionary<string, object> ContextData { get; }
        public DocumentNode Document { get; set; }
        public IOperation Operation { get; set; }
        public QueryValidationResult ValidationResult { get; set; }
        public IVariableCollection Variables { get; set; }
        public CancellationToken RequestAborted { get; set; }
        public IExecutionResult Result { get; set; }
        public Exception Exception { get; set; }

    }
}
