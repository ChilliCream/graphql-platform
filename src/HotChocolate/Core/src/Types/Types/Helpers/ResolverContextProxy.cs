#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution;

internal sealed class ResolverContextProxy : IResolverContext
{
    private readonly IResolverContext _resolverContext;

    public ResolverContextProxy(IResolverContext resolverContext)
    {
        _resolverContext = resolverContext;
        ScopedContextData = resolverContext.ScopedContextData;
        LocalContextData = resolverContext.LocalContextData;
    }

    public ISchema Schema => _resolverContext.Schema;

    public IObjectType ObjectType => _resolverContext.ObjectType;

    public IOperation Operation => _resolverContext.Operation;

    public ISelection Selection => _resolverContext.Selection;

    public IVariableValueCollection Variables => _resolverContext.Variables;

    public Path Path => _resolverContext.Path;

    public T Parent<T>() => _resolverContext.Parent<T>();

    public T ArgumentValue<T>(string name) => _resolverContext.ArgumentValue<T>(name);

    public TValueNode ArgumentLiteral<TValueNode>(string name) where TValueNode : IValueNode
        => _resolverContext.ArgumentLiteral<TValueNode>(name);

    public Optional<T> ArgumentOptional<T>(string name)
        => _resolverContext.ArgumentOptional<T>(name);

    public ValueKind ArgumentKind(string name) => _resolverContext.ArgumentKind(name);

    public T Service<T>() => _resolverContext.Service<T>();

    public T Resolver<T>() => _resolverContext.Resolver<T>();

    public IServiceProvider Services
    {
        get => _resolverContext.Services;
        set => _resolverContext.Services = value;
    }

    public string ResponseName => _resolverContext.ResponseName;

    public bool HasErrors => _resolverContext.HasErrors;

    public IDictionary<string, object?> ContextData => _resolverContext.ContextData;

    public IImmutableDictionary<string, object?> ScopedContextData { get; set; }

    public IImmutableDictionary<string, object?> LocalContextData { get; set; }

    public CancellationToken RequestAborted => _resolverContext.RequestAborted;

    public object Service(Type service) => _resolverContext.Service(service);

    public void ReportError(string errorMessage) => _resolverContext.ReportError(errorMessage);

    public void ReportError(IError error) => _resolverContext.ReportError(error);

    public void ReportError(Exception exception, Action<IErrorBuilder>? configure = null)
        => _resolverContext.ReportError(exception, configure);

    public IReadOnlyList<ISelection> GetSelections(
        IObjectType typeContext,
        ISelection? selection = null,
        bool allowInternals = false)
        => _resolverContext.GetSelections(typeContext, selection, allowInternals);

    public T GetQueryRoot<T>() => _resolverContext.GetQueryRoot<T>();
}
