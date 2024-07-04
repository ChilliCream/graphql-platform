using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Execution;

internal sealed class RequestContext(
    ISchema schema,
    ulong executorVersion,
    IErrorHandler errorHandler,
    IExecutionDiagnosticEvents diagnosticEvents)
    : IRequestContext
{
    private readonly ConcurrentDictionary<string, object?> _contextData = new();
    private DocumentValidatorResult? _validationResult;

    public ISchema Schema { get; } = schema;

    public ulong ExecutorVersion { get; } = executorVersion;

    public int? RequestIndex { get; set; }

    public IServiceProvider Services { get; private set; } = default!;

    public IErrorHandler ErrorHandler { get; } = errorHandler;

    public IExecutionDiagnosticEvents DiagnosticEvents { get; } = diagnosticEvents;

    public IOperationRequest Request { get; private set; } = default!;

    public IDictionary<string, object?> ContextData => _contextData;

    public CancellationToken RequestAborted { get; set; }

    public OperationDocumentId? DocumentId { get; set; }

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

    public IReadOnlyList<IVariableValueCollection>? Variables { get; set; }

    public IExecutionResult? Result { get; set; }

    public Exception? Exception { get; set; }

    public IRequestContext Clone()
    {
        var cloned = new RequestContext(
            Schema,
            ExecutorVersion,
            ErrorHandler,
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
            RequestIndex = RequestIndex,
        };

        foreach (var item in _contextData)
        {
            cloned._contextData.TryAdd(item.Key, item.Value);
        }

        return cloned;
    }

    public void Initialize(IOperationRequest request, IServiceProvider services)
    {
        Request = request;
        Services = services;

        if (request.ContextData is null)
        {
            return;
        }

        foreach (var item in request.ContextData)
        {
            _contextData.TryAdd(item.Key, item.Value);
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
        RequestIndex = default;
    }
}
