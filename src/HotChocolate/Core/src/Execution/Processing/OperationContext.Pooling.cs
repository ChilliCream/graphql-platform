using System.Runtime.CompilerServices;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Fetching;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal sealed partial class OperationContext
{
    private readonly IFactory<ResolverTask> _resolverTaskFactory;
    private readonly WorkScheduler _workScheduler;
    private readonly DeferredWorkScheduler _deferredWorkScheduler;
    private readonly ResultBuilder _resultBuilder;
    private readonly AggregateServiceScopeInitializer _serviceScopeInitializer;
    private RequestContext _requestContext = null!;
    private Schema _schema = null!;
    private IErrorHandler _errorHandler = null!;
    private ResolverProvider _resolvers = null!;
    private IExecutionDiagnosticEvents _diagnosticEvents = null!;
    private IDictionary<string, object?> _contextData = null!;
    private CancellationToken _requestAborted;
    private IOperation _operation = null!;
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
        ResultBuilder resultBuilder,
        ITypeConverter typeConverter,
        AggregateServiceScopeInitializer serviceScopeInitializer)
    {
        _resolverTaskFactory = resolverTaskFactory;
        _workScheduler = new WorkScheduler(this);
        _deferredWorkScheduler = new DeferredWorkScheduler();
        _resultBuilder = resultBuilder;
        _serviceScopeInitializer = serviceScopeInitializer;
        Converter = typeConverter;
    }

    public bool IsInitialized => _isInitialized;

    public void Initialize(
        RequestContext requestContext,
        IServiceProvider scopedServices,
        IBatchDispatcher batchDispatcher,
        IOperation operation,
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

        IncludeFlags = _operation.CreateIncludeFlags(variables);
        _workScheduler.Initialize(batchDispatcher);
        _deferredWorkScheduler.Initialize(this);
        _resultBuilder.Initialize(_requestContext, _diagnosticEvents);

        if (requestContext.RequestIndex != -1)
        {
            _resultBuilder.SetRequestIndex(requestContext.RequestIndex);
        }

        if (variableIndex != -1)
        {
            _resultBuilder.SetVariableIndex(variableIndex);
        }
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
        _workScheduler.Initialize(_batchDispatcher);
        _deferredWorkScheduler.InitializeFrom(this, context._deferredWorkScheduler);
        _resultBuilder.Initialize(_requestContext, _diagnosticEvents);

        if (context._requestContext.RequestIndex != -1)
        {
            _resultBuilder.SetRequestIndex(context._requestContext.RequestIndex);
        }

        if (context._variableIndex != -1)
        {
            _resultBuilder.SetVariableIndex(context._variableIndex);
        }
    }

    public void Clean()
    {
        if (_isInitialized)
        {
            _workScheduler.Clear();
            _resultBuilder.Clear();
            _deferredWorkScheduler.Clear();
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
