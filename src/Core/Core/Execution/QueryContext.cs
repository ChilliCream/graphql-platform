using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    public class QueryContext
        : IQueryContext
    {
        private readonly Dictionary<string, object> _custom =
            new Dictionary<string, object>();

        public QueryContext(
            ISchema schema,
            IServiceProvider services,
            IReadOnlyQueryRequest request)
        {
            Schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            Request = request
                ?? throw new ArgumentNullException(nameof(request));
            Services = services;
        }

        public ISchema Schema { get; }
        public IReadOnlyQueryRequest Request { get; }
        public IServiceProvider Services { get; }

        public IDictionary<string, object> Custom { get; } =
            new Dictionary<string, object>();

        public DocumentNode Document { get; set; }
        public OperationDefinitionNode Operation { get; set; }
        public QueryValidationResult ValidationResult { get; set; }
        public IVariableCollection VariableCollection { get; set; }
        public CancellationToken RequestAborted { get; set; }
        public IExecutionResult Result { get; set; }
        public Exception Exception { get; set; }
    }
}
