using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class Resolver
{
    public Resolver(
        string typeName,
        ISymbol member,
        ResolverResultKind resultKind,
        ImmutableArray<ResolverParameter> parameters,
        ImmutableArray<MemberBinding> bindings,
        ResolverKind kind = ResolverKind.Default,
        FieldFlags flags = FieldFlags.None,
        string? schemaTypeName = null)
    {
        TypeName = typeName;
        Member = member;
        SchemaTypeName = schemaTypeName;
        ResultKind = resultKind;
        Parameters = parameters;
        Bindings = bindings;
        Kind = kind;
        Flags = flags;
    }

    public string TypeName { get; }

    public ISymbol Member { get; }

    public ITypeSymbol ReturnType
        => Member.GetReturnType()
            ?? throw new InvalidOperationException("Resolver has no return type.");

    public ITypeSymbol UnwrappedReturnType
        => ReturnType.UnwrapTaskOrValueTask();

    public string? SchemaTypeName { get; }

    public bool IsStatic => Member.IsStatic;

    public bool IsPure
        => Kind is not ResolverKind.NodeResolver
            && ResultKind is ResolverResultKind.Pure
            && Parameters.All(t => t.IsPure);

    public ResolverKind Kind { get; }

    public FieldFlags Flags { get; }

    public ResolverResultKind ResultKind { get; }

    public ImmutableArray<ResolverParameter> Parameters { get; }

    public bool RequiresParameterBindings => Parameters.Any(t => t.RequiresBinding);

    public ImmutableArray<MemberBinding> Bindings { get; }

    public Resolver WithSchemaTypeName(string? schemaTypeName)
    {
        return new Resolver(
            TypeName,
            Member,
            ResultKind,
            Parameters,
            Bindings,
            Kind,
            Flags,
            schemaTypeName);
    }
}
