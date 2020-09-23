using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Utilities;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    internal sealed class RequestContext : IRequestContext
    {
        public RequestContext(
            ISchema schema,
            IServiceProvider services,
            IErrorHandler errorHandler,
            ITypeConverter converter,
            IActivator activator,
            IDiagnosticEvents diagnosticEvents,
            IQueryRequest request)
        {
            Schema = schema;
            Services = services;
            ErrorHandler = errorHandler;
            Converter = converter;
            Activator = activator;
            DiagnosticEvents = diagnosticEvents;
            Request = request;
            ContextData = request.ContextData is null
                ? new ConcurrentDictionary<string, object?>()
                : new ConcurrentDictionary<string, object?>(request.ContextData);
        }

        public ISchema Schema { get; }

        public IServiceProvider Services { get; }

        public IErrorHandler ErrorHandler { get; }

        public ITypeConverter Converter { get; }

        public IActivator Activator { get; }

        public IDiagnosticEvents DiagnosticEvents { get; }

        public IQueryRequest Request { get; }

        public IDictionary<string, object?> ContextData { get; }

        public CancellationToken RequestAborted { get; set; }

        public string? DocumentId { get; set; }

        public string? DocumentHash { get; set; }

        public bool IsCachedDocument { get; set; }

        public bool IsPersistedDocument { get; set; }

        public DocumentNode? Document { get; set; }

        public DocumentValidatorResult? ValidationResult { get; set; }

        public string? OperationId { get; set; }

        public IPreparedOperation? Operation { get; set; }

        public IVariableValueCollection? Variables { get; set; }

        public IExecutionResult? Result { get; set; }

        public Exception? Exception { get; set; }
    }
}
