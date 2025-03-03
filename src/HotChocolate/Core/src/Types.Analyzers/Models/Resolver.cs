using System.Collections.Immutable;
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
        FieldFlags flags = FieldFlags.None)
    {
        TypeName = typeName;
        Member = member;
        ResultKind = resultKind;
        Parameters = parameters;
        Bindings = bindings;
        Kind = kind;
        Flags = flags;
    }

    public string TypeName { get; }

    public ISymbol Member { get; }

    public bool IsStatic => Member.IsStatic;

    public bool IsPure
        => Kind is not ResolverKind.NodeResolver
            && ResultKind is ResolverResultKind.Pure
            && Parameters.All(t => t.IsPure);

    public ResolverKind Kind { get; }

    public FieldFlags Flags { get; }

    public ResolverResultKind ResultKind { get; }

    public ImmutableArray<ResolverParameter> Parameters { get; }

    public ImmutableArray<MemberBinding> Bindings { get; }
}
