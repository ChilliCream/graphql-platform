using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using GreenDonut.DependencyInjection;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Fetching;
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
    private IRequestContext _requestContext = default!;
    private ISchema _schema = default!;
    private IErrorHandler _errorHandler = default!;
    private ResolverProvider _resolvers = default!;
    private IExecutionDiagnosticEvents _diagnosticEvents = default!;
    private IDictionary<string, object?> _contextData = default!;
    private CancellationToken _requestAborted;
    private IOperation _operation = default!;
    private IVariableValueCollection _variables = default!;
    private IServiceProvider _services = default!;
    private Func<object?> _resolveQueryRootValue = default!;
    private IDataLoaderContext _dataLoaderContext = default!;
    private InputParser _inputParser = default!;
    private object? _rootValue;
    private bool _isInitialized;

    public OperationContext(
        IFactory<ResolverTask> resolverTaskFactory,
        ResultBuilder resultBuilder,
        ITypeConverter typeConverter)
    {
        _resolverTaskFactory = resolverTaskFactory;
        _workScheduler = new(this);
        _deferredWorkScheduler = new();
        _resultBuilder = resultBuilder;
        Converter = typeConverter;
    }

    public bool IsInitialized => _isInitialized;

    public void Initialize(
        IRequestContext requestContext,
        IServiceProvider scopedServices,
        IOperation operation,
        IVariableValueCollection variables,
        object? rootValue,
        Func<object?> resolveQueryRootValue)
    {
        _requestContext = requestContext;
        _schema = requestContext.Schema;
        _errorHandler = requestContext.ErrorHandler;
        _resolvers = scopedServices.GetRequiredService<ResolverProvider>();
        _diagnosticEvents = requestContext.DiagnosticEvents;
        _contextData = requestContext.ContextData;
        _requestAborted = requestContext.RequestAborted;
        _operation = operation;
        _variables = variables;
        _services = scopedServices;
        _inputParser = scopedServices.GetRequiredService<InputParser>();
        _rootValue = rootValue;
        _resolveQueryRootValue = resolveQueryRootValue;
        _dataLoaderContext = scopedServices.GetRequiredService<IDataLoaderContext>();
        _isInitialized = true;

        IncludeFlags = _operation.CreateIncludeFlags(variables);
        _workScheduler.Initialize();
        _deferredWorkScheduler.Initialize(this);
        _resultBuilder.Initialize(_requestContext, _diagnosticEvents);
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
        _dataLoaderContext = context._dataLoaderContext;
        _isInitialized = true;

        IncludeFlags = _operation.CreateIncludeFlags(_variables);
        _workScheduler.Initialize();
        _deferredWorkScheduler.InitializeFrom(this, context._deferredWorkScheduler);
        _resultBuilder.Initialize(_requestContext, _diagnosticEvents);
    }

    public void Clean()
    {
        if (_isInitialized)
        {
            _workScheduler.Clear();
            _resultBuilder.Clear();
            _deferredWorkScheduler.Clear();
            _requestContext = default!;
            _schema = default!;
            _errorHandler = default!;
            _resolvers = default!;
            _diagnosticEvents = default!;
            _contextData = default!;
            _operation = default!;
            _variables = default!;
            _services = default!;
            _rootValue = null;
            _resolveQueryRootValue = default!;
            _dataLoaderContext = default!;
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
