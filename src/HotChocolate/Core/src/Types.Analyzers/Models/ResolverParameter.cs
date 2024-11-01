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

    public IParameterSymbol Parameter { get; }

    public ResolverParameterKind Kind { get; }

    public bool IsPure
        => Kind == ResolverParameterKind.Argument ||
            Kind == ResolverParameterKind.Parent ||
            Kind == ResolverParameterKind.Service ||
            Kind == ResolverParameterKind.GetGlobalState ||
            Kind == ResolverParameterKind.SetGlobalState ||
            Kind == ResolverParameterKind.GetScopedState ||
            Kind == ResolverParameterKind.HttpContext ||
            Kind == ResolverParameterKind.HttpRequest ||
            Kind == ResolverParameterKind.HttpResponse ||
            Kind == ResolverParameterKind.DocumentNode ||
            Kind == ResolverParameterKind.EventMessage ||
            Kind == ResolverParameterKind.FieldNode ||
            Kind == ResolverParameterKind.OutputField ||
            Kind == ResolverParameterKind.ClaimsPrincipal;

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

        return ResolverParameterKind.Unknown;
    }
}
