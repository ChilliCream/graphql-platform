#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
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

    public IDictionary<string, object?> ContextData => _resolverContext.ContextData;

    public ISchema Schema => _resolverContext.Schema;

    public IObjectType RootType => _resolverContext.RootType;

    public IObjectType ObjectType => _resolverContext.ObjectType;

    public IServiceProvider Services
    {
        get => _resolverContext.Services;
        set => _resolverContext.Services = value;
    }

    [Obsolete]
    public IObjectField Field => _resolverContext.Field;

    public DocumentNode Document => _resolverContext.Document;

    public OperationDefinitionNode Operation => _resolverContext.Operation;

    [Obsolete]
    public FieldNode FieldSelection => _resolverContext.FieldSelection;

    public NameString ResponseName => _resolverContext.ResponseName;

    public Path Path => _resolverContext.Path;

    public bool HasErrors => _resolverContext.HasErrors;

    public IFieldSelection Selection => _resolverContext.Selection;

    public IVariableValueCollection Variables => _resolverContext.Variables;


    public IImmutableDictionary<string, object?> ScopedContextData
    {
        get;
        set;
    }

    public IImmutableDictionary<string, object?> LocalContextData
    {
        get;
        set;
    }

    public CancellationToken RequestAborted => _resolverContext.RequestAborted;

    [Obsolete("Use ArgumentValue<T>(name) or " +
        "ArgumentLiteral<TValueNode>(name) or " +
        "ArgumentOptional<T>(name).")]
    public T? Argument<T>(NameString name) => _resolverContext.Argument<T>(name);

    public object Service(Type service) => _resolverContext.Service(service);

    public void ReportError(string errorMessage) => _resolverContext.ReportError(errorMessage);

    public void ReportError(IError error) => _resolverContext.ReportError(error);

    public void ReportError(Exception exception, Action<IErrorBuilder>? configure = null)
        => _resolverContext.ReportError(exception, configure);

    public IReadOnlyList<IFieldSelection> GetSelections(
        ObjectType typeContext,
        SelectionSetNode? selectionSet = null,
        bool allowInternals = false)
        => _resolverContext.GetSelections(typeContext, selectionSet, allowInternals);

    public T GetQueryRoot<T>()
        => _resolverContext.GetQueryRoot<T>();

    public T Parent<T>() => _resolverContext.Parent<T>();

    public T ArgumentValue<T>(NameString name) => _resolverContext.ArgumentValue<T>(name);

    public TValueNode ArgumentLiteral<TValueNode>(NameString name)
        where TValueNode : IValueNode
        => _resolverContext.ArgumentLiteral<TValueNode>(name);

    public Optional<T> ArgumentOptional<T>(NameString name)
        => _resolverContext.ArgumentOptional<T>(name);

    public ValueKind ArgumentKind(NameString name) => _resolverContext.ArgumentKind(name);

    public T Service<T>() => _resolverContext.Service<T>();

    public T Resolver<T>() => _resolverContext.Resolver<T>();
}
