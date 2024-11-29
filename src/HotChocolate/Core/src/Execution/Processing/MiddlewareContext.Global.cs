using HotChocolate.Execution.Properties;
using HotChocolate.Execution.Serialization;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Processing;

internal partial class MiddlewareContext : IMiddlewareContext
{
    private readonly OperationResultBuilderFacade _operationResultBuilder = new();
    private readonly List<Func<ValueTask>> _cleanupTasks = [];
    private OperationContext _operationContext = default!;
    private IServiceProvider _services = default!;
    private InputParser _parser = default!;
    private object? _resolverResult;
    private bool _hasResolverResult;

    public IServiceProvider Services
    {
        get => _services;
        set => _services = value ?? throw new ArgumentNullException(nameof(value));
    }

    public IServiceProvider RequestServices => _operationContext.Services;

    public ISchema Schema => _operationContext.Schema;

    public IOperation Operation => _operationContext.Operation;

    public IOperationResultBuilder OperationResult => _operationResultBuilder;

    public IDictionary<string, object?> ContextData => _operationContext.ContextData;

    public IVariableValueCollection Variables => _operationContext.Variables;

    public CancellationToken RequestAborted { get; private set; }

    public bool HasCleanupTasks => _cleanupTasks.Count > 0;

    public void ReportError(string errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
        {
            throw new ArgumentException(
                Resources.MiddlewareContext_ReportErrorCannotBeNull,
                nameof(errorMessage));
        }

        ReportError(
            ErrorBuilder.New()
                .SetMessage(errorMessage)
                .SetPath(Path)
                .SetLocations([_selection.SyntaxNode])
                .Build());
    }

    public void ReportError(Exception exception, Action<IErrorBuilder>? configure = null)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        if (exception is GraphQLException ex)
        {
            foreach (var error in ex.Errors)
            {
                ReportError(error);
            }
        }
        else if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                ReportError(innerException);
            }
        }
        else
        {
            var errorBuilder = _operationContext.ErrorHandler
                .CreateUnexpectedError(exception)
                .SetPath(Path)
                .SetLocations([_selection.SyntaxNode]);

            configure?.Invoke(errorBuilder);

            ReportError(errorBuilder.Build());
        }
    }

    public void ReportError(IError error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        if (error is AggregateError aggregateError)
        {
            foreach (var innerError in aggregateError.Errors)
            {
                ReportSingle(innerError);
            }
        }
        else
        {
            ReportSingle(error);
        }

        return;

        void ReportSingle(IError singleError)
        {
            var handled = _operationContext.ErrorHandler.Handle(singleError);

            if (handled is AggregateError ar)
            {
                foreach (var ie in ar.Errors)
                {
                    var errorWithPath = EnsurePathAndLocation(ie, _selection.SyntaxNode, Path);
                    _operationContext.Result.AddError(errorWithPath, _selection);
                    _operationContext.DiagnosticEvents.ResolverError(this, errorWithPath);
                }
            }
            else
            {
                var errorWithPath = EnsurePathAndLocation(handled, _selection.SyntaxNode, Path);
                _operationContext.Result.AddError(errorWithPath, _selection);
                _operationContext.DiagnosticEvents.ResolverError(this, errorWithPath);
            }

            HasErrors = true;
        }

        static IError EnsurePathAndLocation(IError error, ISyntaxNode node, Path path)
        {
            if (error.Path is null)
            {
                error = error.WithPath(path);
            }

            if (error.Locations is not { Count: > 0 } && node.Location is not null)
            {
                error = error.WithLocations([new Location(node.Location.Line, node.Location.Column)]);
            }

            return error;
        }
    }

    public async ValueTask<T> ResolveAsync<T>()
    {
        if (!_hasResolverResult)
        {
            _resolverResult = Field.Resolver is null
                ? null
                : await Field.Resolver(this).ConfigureAwait(false);
            _hasResolverResult = true;
        }

        return _resolverResult is null
            ? default!
            : (T)_resolverResult;
    }

    public T Resolver<T>()
        => _operationContext.Resolvers.GetResolver<T>(_operationContext.Services);

    public T Service<T>() where T : notnull => Services.GetRequiredService<T>();

    public T Service<T>(object key) where T : notnull => Services.GetRequiredKeyedService<T>(key);

    public object Service(Type service)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        return Services.GetRequiredService(service);
    }

    public void RegisterForCleanup(
        Func<ValueTask> action,
        CleanAfter cleanAfter = CleanAfter.Resolver)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (cleanAfter is CleanAfter.Request)
        {
            _operationContext.Result.RegisterForCleanup(action);
        }
        else
        {
            _cleanupTasks.Add(action);
        }
    }

    public async ValueTask ExecuteCleanupTasksAsync()
    {
        var count = _cleanupTasks.Count;

        if (count == 1)
        {
            await _cleanupTasks[0].Invoke().ConfigureAwait(false);
            return;
        }

        if (count == 2)
        {
            await _cleanupTasks[0].Invoke().ConfigureAwait(false);
            await _cleanupTasks[1].Invoke().ConfigureAwait(false);
            return;
        }

        if (count == 3)
        {
            await _cleanupTasks[0].Invoke().ConfigureAwait(false);
            await _cleanupTasks[1].Invoke().ConfigureAwait(false);
            await _cleanupTasks[2].Invoke().ConfigureAwait(false);
            return;
        }

        for (var i = 0; i < count; i++)
        {
            await _cleanupTasks[i].Invoke().ConfigureAwait(false);
        }
    }

    public T GetQueryRoot<T>()
        => _operationContext.GetQueryRoot<T>();

    public IMiddlewareContext Clone()
    {
        // The middleware context is bound to a resolver task,
        // so we need to create a resolver task in order to clone
        // this context.
        var resolverTask =
            _operationContext.CreateResolverTask(
                Selection,
                _parent,
                ParentResult,
                ResponseIndex,
                ScopedContextData);

        // We need to manually copy the local state.
        resolverTask.Context.LocalContextData = LocalContextData;

        // Since resolver tasks are pooled and returned to the pool after they are executed
        // we need to complete the task manually when the resolver task of the current context
        // is completed.
        RegisterForCleanup(() => resolverTask.CompleteUnsafeAsync());

        return resolverTask.Context;
    }

    IResolverContext IResolverContext.Clone()
        => Clone();

    private sealed class OperationResultBuilderFacade : IOperationResultBuilder
    {
        public OperationContext Context { get; set; } = default!;

        public void SetResultState(string key, object? value)
            => Context.Result.SetContextData(key, value);

        public void SetResultState(string key, UpdateState<object?> value)
            => Context.Result.SetContextData(key, value);

        public void SetResultState<TState>(
            string key,
            TState state,
            UpdateState<object?, TState> value)
            => Context.Result.SetContextData(key, state, value);

        public void SetExtension<TValue>(string key, TValue value)
            => Context.Result.SetExtension(key, new NeedsFormatting<TValue>(value));

        public void SetExtension<TValue>(string key, UpdateState<TValue> value)
            => Context.Result.SetExtension<NeedsFormatting<TValue>?>(
                key,
                (k, c) => new NeedsFormatting<TValue>(
                    value(
                        k,
                        c is null
                            ? default!
                            : c.Value)));

        public void SetExtension<TValue, TState>(
            string key,
            TState state,
            UpdateState<TValue, TState> value)
            => Context.Result.SetExtension<NeedsFormatting<TValue>?, TState>(
                key,
                state,
                (k, c, s) => new NeedsFormatting<TValue>(
                    value(
                        k,
                        c is null
                            ? default!
                            : c.Value,
                        s)));
    }
}
