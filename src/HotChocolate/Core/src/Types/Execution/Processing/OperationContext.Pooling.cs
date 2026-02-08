using System.Runtime.CompilerServices;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Fetching;
using HotChocolate.Resolvers;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal sealed partial class OperationContext
{
    private readonly IFactory<ResolverTask> _resolverTaskFactory;
    private readonly WorkScheduler _workScheduler;
    private WorkScheduler _currentWorkScheduler;
    private readonly AggregateServiceScopeInitializer _serviceScopeInitializer;
    private RequestContext _requestContext = null!;
    private Schema _schema = null!;
    private IErrorHandler _errorHandler = null!;
    private ResolverProvider _resolvers = null!;
    private IExecutionDiagnosticEvents _diagnosticEvents = null!;
    private IDictionary<string, object?> _contextData = null!;
    private CancellationToken _requestAborted;
    private Operation _operation = null!;
    private IVariableValueCollection _variables = null!;
    private IServiceProvider _services = null!;
    private Func<object?> _resolveQueryRootValue = null!;
    private IBatchDispatcher _batchDispatcher = null!;
    private InputParser _inputParser = null!;
    private int _variableIndex;
    private object? _rootValue;
    private bool _isInitialized;

    public OperationContext(
        IFactory<ResolverTask> resolverTaskFactory,
        ITypeConverter typeConverter,
        AggregateServiceScopeInitializer serviceScopeInitializer)
    {
        _resolverTaskFactory = resolverTaskFactory;
        _workScheduler = new WorkScheduler(this);
        _currentWorkScheduler = _workScheduler;
        _serviceScopeInitializer = serviceScopeInitializer;
        Converter = typeConverter;
    }

    public bool IsInitialized => _isInitialized;

    public bool IsSharedScheduler => !ReferenceEquals(_workScheduler, _currentWorkScheduler);

    public void Initialize(
        RequestContext requestContext,
        IServiceProvider scopedServices,
        IBatchDispatcher batchDispatcher,
        Operation operation,
        IVariableValueCollection variables,
        object? rootValue,
        Func<object?> resolveQueryRootValue,
        int variableIndex = -1)
    {
        _requestContext = requestContext;
        _schema = Unsafe.As<Schema>(requestContext.Schema);
        _errorHandler = _schema.Services.GetRequiredService<IErrorHandler>();
        _resolvers = scopedServices.GetRequiredService<ResolverProvider>();
        _diagnosticEvents = _schema.Services.GetRequiredService<IExecutionDiagnosticEvents>();
        _contextData = requestContext.ContextData;
        _requestAborted = requestContext.RequestAborted;
        _operation = operation;
        _variables = variables;
        _services = scopedServices;
        _inputParser = scopedServices.GetRequiredService<InputParser>();
        _rootValue = rootValue;
        _resolveQueryRootValue = resolveQueryRootValue;
        _batchDispatcher = batchDispatcher;
        _variableIndex = variableIndex;
        _isInitialized = true;

        IncludeFlags = operation.CreateIncludeFlags(variables);
        Result.Data = new ResultDocument(operation, IncludeFlags);
        Result.RequestIndex = _requestContext.RequestIndex;
        Result.VariableIndex = variableIndex;

        _workScheduler.Initialize(batchDispatcher);
        _currentWorkScheduler = _workScheduler;
    }

    public void InitializeFrom(OperationContext context)
    {
        _requestContext = context._requestContext;
        _schema = context._schema;
        _errorHandler = context._errorHandler;
        _resolvers = context._resolvers;
        _diagnosticEvents = context._diagnosticEvents;
        _contextData = context.ContextData;
        _requestAborted = context._requestAborted;
        _operation = context._operation;
        _variables = context._variables;
        _services = context._services;
        _inputParser = context._inputParser;
        _rootValue = context._rootValue;
        _resolveQueryRootValue = context._resolveQueryRootValue;
        _batchDispatcher = context._batchDispatcher;
        _isInitialized = true;

        IncludeFlags = _operation.CreateIncludeFlags(_variables);
        Result.Data = new ResultDocument(_operation, IncludeFlags);
        Result.RequestIndex = _requestContext.RequestIndex;
        Result.VariableIndex = context._variableIndex;

        _workScheduler.Initialize(_batchDispatcher);
        _currentWorkScheduler = _workScheduler;
    }

    public void Clean()
    {
        if (_isInitialized)
        {
            _currentWorkScheduler = _workScheduler;
            _workScheduler.Clear();
            _requestContext = null!;
            _schema = null!;
            _errorHandler = null!;
            _resolvers = null!;
            _diagnosticEvents = null!;
            _contextData = null!;
            _operation = null!;
            _variables = null!;
            _services = null!;
            _rootValue = null;
            _resolveQueryRootValue = null!;
            _batchDispatcher = null!;
            _isInitialized = false;
            Result.Reset();
        }
    }

    public void ResetScheduler()
    {
        if (_isInitialized)
        {
            _currentWorkScheduler = _workScheduler;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertInitialized()
    {
        if (!_isInitialized)
        {
            throw Object_Not_Initialized();
        }
    }
}
