using System.Collections.Concurrent;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Execution;

internal sealed class DefaultRequestContext(
    Schema schema,
    ulong executorVersion,
    IErrorHandler errorHandler,
    IExecutionDiagnosticEvents diagnosticEvents)
    : IRequestContext
{
    private readonly ConcurrentDictionary<string, object?> _contextData = new();
    private readonly RequestFeatureCollection _features = new();
    private DocumentValidatorResult? _validationResult;

    public Schema Schema { get; } = schema;

    public ulong ExecutorVersion { get; } = executorVersion;

    public int? RequestIndex { get; set; }

    public IServiceProvider Services { get; private set; } = null!;

    public IErrorHandler ErrorHandler { get; } = errorHandler;

    public IExecutionDiagnosticEvents DiagnosticEvents { get; } = diagnosticEvents;

    public IOperationRequest Request { get; private set; } = null!;

    public IDictionary<string, object?> ContextData => _contextData;

    public IFeatureCollection Features => _features;

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

    public void Initialize(IOperationRequest request, IServiceProvider services)
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

        _features.Parent = request.Features;
    }

    public void Reset()
    {
        if (_contextData.Count != 0)
        {
            _contextData.Clear();
        }

        Request = null!;
        Services = null!;
        RequestAborted = CancellationToken.None;
        DocumentId = null;
        DocumentHash = null;
        IsCachedDocument = false;
        IsPersistedDocument = false;
        Document = null;
        ValidationResult = null;
        IsValidDocument = false;
        OperationId = null;
        Operation = null;
        Variables = null;
        Result = null;
        Exception = null;
        RequestIndex = null;
        _features.Parent = null;
    }
}
