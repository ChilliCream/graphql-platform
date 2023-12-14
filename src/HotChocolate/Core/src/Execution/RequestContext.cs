using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Execution;

internal sealed class RequestContext : IRequestContext
{
    private readonly ConcurrentDictionary<string, object?> _contextData = new();
    private DocumentValidatorResult? _validationResult;

    public RequestContext(
        ISchema schema,
        ulong executorVersion,
        IErrorHandler errorHandler,
        IActivator activator,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        Schema = schema;
        ExecutorVersion = executorVersion;
        ErrorHandler = errorHandler;
        Activator = activator;
        DiagnosticEvents = diagnosticEvents;
    }

    public ISchema Schema { get; }

    public ulong ExecutorVersion { get; }

    public IServiceProvider Services { get; private set; } = default!;

    public IErrorHandler ErrorHandler { get; }

    public IActivator Activator { get; }

    public IExecutionDiagnosticEvents DiagnosticEvents { get; }

    public IQueryRequest Request { get; private set; } = default!;

    public IDictionary<string, object?> ContextData => _contextData;

    public CancellationToken RequestAborted { get; set; }

    public string? DocumentId { get; set; }

    public string? DocumentHash { get; set; }

    public DocumentNode? Document { get; set; }

    public bool IsCachedDocument { get; set; }

    public bool IsPersistedDocument { get; set; }

    public DocumentValidatorResult? ValidationResult
    {
        get => _validationResult;
        set
        {
            _validationResult = value;
            IsValidDocument = !value?.HasErrors ?? false;
        }
    }

    public bool IsValidDocument { get; private set; }

    public string? OperationId { get; set; }

    public IOperation? Operation { get; set; }

    public IVariableValueCollection? Variables { get; set; }

    public IExecutionResult? Result { get; set; }

    public Exception? Exception { get; set; }

    public IRequestContext Clone()
    {
        var cloned = new RequestContext(
            Schema,
            ExecutorVersion,
            ErrorHandler,
            Activator,
            DiagnosticEvents)
        {
            Request = Request,
            Services = Services,
            RequestAborted = RequestAborted,
            DocumentId = DocumentId,
            DocumentHash = DocumentHash,
            IsCachedDocument = IsCachedDocument,
            Document = Document,
            ValidationResult = ValidationResult,
            OperationId = OperationId,
            Operation = Operation,
            Variables = Variables,
            Result = Result,
            Exception = Exception,
        };

        foreach (var item in _contextData)
        {
            cloned._contextData.TryAdd(item.Key, item.Value);
        }

        return cloned;
    }

    public void Initialize(IQueryRequest request, IServiceProvider services)
    {
        Request = request;
        Services = services;

        if (request.ContextData is not null)
        {
            foreach (var item in request.ContextData)
            {
                _contextData.TryAdd(item.Key, item.Value);
            }
        }
    }

    public void Reset()
    {
        if (_contextData.Count != 0)
        {
            _contextData.Clear();
        }

        Request = default!;
        Services = default!;
        RequestAborted = default;
        DocumentId = default;
        DocumentHash = default;
        IsCachedDocument = false;
        Document = default;
        ValidationResult = default;
        IsValidDocument = false;
        OperationId = default;
        Operation = default;
        Variables = default;
        Result = default;
        Exception = default;
    }
}
