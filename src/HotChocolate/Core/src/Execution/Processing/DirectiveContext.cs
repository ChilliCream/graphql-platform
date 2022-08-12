using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal sealed class DirectiveContext : IDirectiveContext
{
    private readonly IMiddlewareContext _middlewareContext;

    public DirectiveContext(IMiddlewareContext middlewareContext, IDirective directive)
    {
        _middlewareContext = middlewareContext;
        Directive = directive;
    }

    public IDirective Directive { get; }

   public ISchema Schema => _middlewareContext.Schema;

    public IObjectType ObjectType => _middlewareContext.ObjectType;

    public IOperation Operation => _middlewareContext.Operation;

    public ISelection Selection => _middlewareContext.Selection;

    public IVariableValueCollection Variables => _middlewareContext.Variables;

    public Path Path => _middlewareContext.Path;

    public T Parent<T>() => _middlewareContext.Parent<T>();

    public T ArgumentValue<T>(string name) => _middlewareContext.ArgumentValue<T>(name);

    public TValueNode ArgumentLiteral<TValueNode>(string name) where TValueNode : IValueNode
        => _middlewareContext.ArgumentLiteral<TValueNode>(name);

    public Optional<T> ArgumentOptional<T>(string name)
        => _middlewareContext.ArgumentOptional<T>(name);

    public ValueKind ArgumentKind(string name) => _middlewareContext.ArgumentKind(name);

    public T Service<T>() => _middlewareContext.Service<T>();

    public T Resolver<T>() => _middlewareContext.Resolver<T>();

    public IServiceProvider Services
    {
        get => _middlewareContext.Services;
        set => _middlewareContext.Services = value;
    }

    public string ResponseName => _middlewareContext.ResponseName;

    public bool HasErrors => _middlewareContext.HasErrors;

    public IDictionary<string, object?> ContextData => _middlewareContext.ContextData;

    public IImmutableDictionary<string, object?> ScopedContextData
    {
        get => _middlewareContext.ScopedContextData;
        set => _middlewareContext.ScopedContextData = value;
    }

    public IImmutableDictionary<string, object?> LocalContextData
    {
        get => _middlewareContext.LocalContextData;
        set => _middlewareContext.LocalContextData = value;
    }

    public CancellationToken RequestAborted => _middlewareContext.RequestAborted;

    public object Service(Type service) => _middlewareContext.Service(service);

    public void ReportError(string errorMessage) => _middlewareContext.ReportError(errorMessage);

    public void ReportError(IError error) => _middlewareContext.ReportError(error);

    public void ReportError(Exception exception, Action<IErrorBuilder>? configure = null)
        => _middlewareContext.ReportError(exception, configure);

    public IReadOnlyList<ISelection> GetSelections(
        IObjectType typeContext,
        ISelection? selection = null,
        bool allowInternals = false)
        => _middlewareContext.GetSelections(typeContext, selection, allowInternals);

    public T GetQueryRoot<T>() => _middlewareContext.GetQueryRoot<T>();

    public IType? ValueType
    {
        get => _middlewareContext.ValueType;
        set => _middlewareContext.ValueType = value;
    }

    public object? Result
    {
        get => _middlewareContext.Result;
        set => _middlewareContext.Result = value;
    }

    public bool IsResultModified => _middlewareContext.IsResultModified;

    public ValueTask<T> ResolveAsync<T>()
        => _middlewareContext.ResolveAsync<T>();

    public void RegisterForCleanup(
        Func<ValueTask> action,
        CleanAfter cleanAfter = CleanAfter.Resolver)
        => _middlewareContext.RegisterForCleanup(action, cleanAfter);

    public IReadOnlyDictionary<string, ArgumentValue> ReplaceArguments(
        IReadOnlyDictionary<string, ArgumentValue> argumentValues)
        => _middlewareContext.ReplaceArguments(argumentValues);
}
