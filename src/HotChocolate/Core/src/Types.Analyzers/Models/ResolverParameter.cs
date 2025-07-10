using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class ResolverParameter
{
    public ResolverParameter(IParameterSymbol parameter, string? key, ResolverParameterKind kind)
    {
        Parameter = parameter;
        Kind = kind;
        Name = parameter.Name;
        Key = key;
        IsNullable = !parameter.IsNonNullable();
    }

    public string Name { get; }

    public string? Key { get; }

    public ITypeSymbol Type => Parameter.Type;

    public ImmutableArray<ITypeSymbol> TypeParameters
        => GetGenericTypeArgument(Type);

    public IParameterSymbol Parameter { get; }

    public ResolverParameterKind Kind { get; }

    public bool IsPure
        => Kind is ResolverParameterKind.Argument
            or ResolverParameterKind.Parent
            or ResolverParameterKind.Service
            or ResolverParameterKind.GetGlobalState
            or ResolverParameterKind.SetGlobalState
            or ResolverParameterKind.GetScopedState
            or ResolverParameterKind.HttpContext
            or ResolverParameterKind.HttpRequest
            or ResolverParameterKind.HttpResponse
            or ResolverParameterKind.DocumentNode
            or ResolverParameterKind.EventMessage
            or ResolverParameterKind.FieldNode
            or ResolverParameterKind.OutputField
            or ResolverParameterKind.ClaimsPrincipal
            or ResolverParameterKind.ConnectionFlags;

    public bool RequiresBinding
        => Kind == ResolverParameterKind.Unknown;

    public bool IsNullable { get; }

    public static ResolverParameter Create(IParameterSymbol parameter, Compilation compilation)
    {
        var kind = GetParameterKind(parameter, compilation, out var key);
        return new ResolverParameter(parameter, key, kind);
    }

    private static ResolverParameterKind GetParameterKind(
        IParameterSymbol parameter,
        Compilation compilation,
        out string? key)
    {
        key = null;

        if (parameter.IsParent())
        {
            return ResolverParameterKind.Parent;
        }

        if (parameter.IsCancellationToken())
        {
            return ResolverParameterKind.CancellationToken;
        }

        if (parameter.IsClaimsPrincipal())
        {
            return ResolverParameterKind.ClaimsPrincipal;
        }

        if (parameter.IsDocumentNode())
        {
            return ResolverParameterKind.DocumentNode;
        }

        if (parameter.IsEventMessage())
        {
            return ResolverParameterKind.EventMessage;
        }

        if (parameter.IsFieldNode())
        {
            return ResolverParameterKind.FieldNode;
        }

        if (parameter.IsOutputField(compilation))
        {
            return ResolverParameterKind.OutputField;
        }

        if (parameter.IsHttpContext())
        {
            return ResolverParameterKind.HttpContext;
        }

        if (parameter.IsHttpRequest())
        {
            return ResolverParameterKind.HttpRequest;
        }

        if (parameter.IsHttpResponse())
        {
            return ResolverParameterKind.HttpResponse;
        }

        if (parameter.IsGlobalState(out key))
        {
            return parameter.IsSetState()
                ? ResolverParameterKind.SetGlobalState
                : ResolverParameterKind.GetGlobalState;
        }

        if (parameter.IsScopedState(out key))
        {
            return parameter.IsSetState()
                ? ResolverParameterKind.SetScopedState
                : ResolverParameterKind.GetScopedState;
        }

        if (parameter.IsLocalState(out key))
        {
            return parameter.IsSetState()
                ? ResolverParameterKind.SetLocalState
                : ResolverParameterKind.GetLocalState;
        }

        if (parameter.IsService(out key))
        {
            return ResolverParameterKind.Service;
        }

        if (parameter.IsArgument(out key))
        {
            return ResolverParameterKind.Argument;
        }

        if (parameter.IsQueryContext())
        {
            return ResolverParameterKind.QueryContext;
        }

        if (parameter.IsPagingArguments())
        {
            return ResolverParameterKind.PagingArguments;
        }

        if (compilation.IsConnectionFlagsType(parameter.Type))
        {
            return ResolverParameterKind.ConnectionFlags;
        }

        return ResolverParameterKind.Unknown;
    }

    private static ImmutableArray<ITypeSymbol> GetGenericTypeArgument(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType)
        {
            return namedTypeSymbol.TypeArguments;
        }

        // Return null if it's not a generic type or index is out of bounds
        return [];
    }
}
