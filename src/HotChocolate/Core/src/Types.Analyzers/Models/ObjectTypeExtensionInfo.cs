using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class ObjectTypeExtensionInfo(
    INamedTypeSymbol type,
    INamedTypeSymbol runtimeType,
    IMethodSymbol? nodeResolver,
    ImmutableArray<ISymbol> members,
    ImmutableArray<Diagnostic> diagnostics,
    ClassDeclarationSyntax classDeclarationSyntax,
    ImmutableArray<Resolver> resolvers)
    : ISyntaxInfo
{
    public string Name { get; } = type.ToFullyQualified();

    public INamedTypeSymbol Type { get; } = type;

    public INamedTypeSymbol RuntimeType { get; } = runtimeType;

    public IMethodSymbol? NodeResolver { get; } = nodeResolver;

    public ImmutableArray<ISymbol> Members { get; } = members;

    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;

    public ClassDeclarationSyntax ClassDeclarationSyntax { get; } = classDeclarationSyntax;

    public ImmutableArray<Resolver> Resolvers { get; } = resolvers;

    public override bool Equals(object? obj)
        => obj is ObjectTypeExtensionInfo other && Equals(other);

    public bool Equals(ISyntaxInfo obj)
        => obj is ObjectTypeExtensionInfo other && Equals(other);

    private bool Equals(ObjectTypeExtensionInfo other)
        => string.Equals(Name, other.Name, StringComparison.Ordinal) &&
            ClassDeclarationSyntax.SyntaxTree.IsEquivalentTo(
                other.ClassDeclarationSyntax.SyntaxTree);

    public override int GetHashCode()
        => HashCode.Combine(Name, ClassDeclarationSyntax);
}

public sealed class Resolver(
    string typeName,
    ISymbol member,
    ResolverResultKind resultKind,
    ImmutableArray<ResolverParameter> parameters)
{
    public string TypeName => typeName;

    public ISymbol Member => member;

    public bool IsPure
        => resultKind is ResolverResultKind.Pure && parameters.All(t => t.IsPure);

    public ResolverResultKind ResultKind => resultKind;

    public ImmutableArray<ResolverParameter> Parameters => parameters;
}

public sealed class ResolverParameter
{
    private ResolverParameter(IParameterSymbol parameter, string? key, ResolverParameterKind kind)
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
                : ResolverParameterKind.GetGlobalState;
        }

        if (parameter.IsLocalState(out key))
        {
            return parameter.IsSetState()
                ? ResolverParameterKind.SetGlobalState
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

public enum ResolverParameterKind
{
    Unknown,
    Parent,
    CancellationToken,
    ClaimsPrincipal,
    DocumentNode,
    EventMessage,
    FieldNode,
    OutputField,
    HttpContext,
    HttpRequest,
    HttpResponse,
    GetGlobalState,
    SetGlobalState,
    GetScopedState,
    SetScopedState,
    GetLocalState,
    SetLocalState,
    Service,
    Argument
}
