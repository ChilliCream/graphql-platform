using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Processing;

internal partial class MiddlewareContext : IMiddlewareContext
{
    private readonly List<Func<ValueTask>> _cleanupTasks = new();
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

    public ISchema Schema => _operationContext.Schema;

    public IOperation Operation => _operationContext.Operation;

    public IDictionary<string, object?> ContextData => _operationContext.ContextData;

    public IVariableValueCollection Variables => _operationContext.Variables;

    IReadOnlyDictionary<string, object?> IPureResolverContext.ScopedContextData
        => ScopedContextData;

    public CancellationToken RequestAborted { get; private set; }

    public bool HasCleanupTasks => _cleanupTasks.Count > 0;

    public IReadOnlyList<ISelection> GetSelections(
        IObjectType typeContext,
        ISelection? selection = null,
        bool allowInternals = false)
    {
        if (typeContext is null)
        {
            throw new ArgumentNullException(nameof(typeContext));
        }

        selection ??= _selection;

        if (selection.SelectionSet is null)
        {
            return Array.Empty<ISelection>();
        }

        var fields = _operationContext.CollectFields(selection, typeContext);

        if (fields.IsConditional)
        {
            var finalFields = new List<ISelection>();

            for (var i = 0; i < fields.Selections.Count; i++)
            {
                var childSelection = fields.Selections[i];
                if (childSelection.IsIncluded(_operationContext.IncludeFlags, allowInternals))
                {
                    finalFields.Add(childSelection);
                }
            }

            return finalFields;
        }

        return fields.Selections;
    }

    public void ReportError(string errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
        {
            throw new ArgumentException(
                Resources.MiddlewareContext_ReportErrorCannotBeNull,
                nameof(errorMessage));
        }

        ReportError(ErrorBuilder.New()
            .SetMessage(errorMessage)
            .SetPath(Path)
            .AddLocation(_selection.SyntaxNode)
            .Build());
    }

    public void ReportError(Exception exception, Action<IErrorBuilder>? configure = null)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        if (exception is GraphQLException graphQLException)
        {
            foreach (var error in graphQLException.Errors)
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
                .AddLocation(_selection.SyntaxNode);

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

        void ReportSingle(IError singleError)
        {
            var handled = _operationContext.ErrorHandler.Handle(singleError);

            if (handled is AggregateError ar)
            {
                foreach (var ie in ar.Errors)
                {
                    _operationContext.Result.AddError(ie, _selection);
                    _operationContext.DiagnosticEvents.ResolverError(this, ie);
                }
            }
            else
            {
                _operationContext.Result.AddError(handled, _selection);
                _operationContext.DiagnosticEvents.ResolverError(this, handled);
            }

            HasErrors = true;
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

        return _resolverResult is null ? default! : (T)_resolverResult;
    }

    public T Resolver<T>() =>
        _operationContext.Activator.GetOrCreate<T>(_operationContext.Services);

    public T Service<T>() => Services.GetRequiredService<T>();

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
        foreach (var task in _cleanupTasks)
        {
            await task.Invoke().ConfigureAwait(false);
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
                Path,
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
}
