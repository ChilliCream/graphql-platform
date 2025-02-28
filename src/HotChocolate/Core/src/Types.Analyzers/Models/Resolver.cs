using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class Resolver(
    string typeName,
    ISymbol member,
    ResolverResultKind resultKind,
    ImmutableArray<ResolverParameter> parameters,
    ImmutableArray<MemberBinding> bindings,
    ResolverKind kind = ResolverKind.Default)
{
    public string TypeName => typeName;

    public ISymbol Member => member;

    public bool IsPure
        => kind is not ResolverKind.NodeResolver
            && resultKind is ResolverResultKind.Pure
            && parameters.All(t => t.IsPure);

    public ResolverKind Kind => kind;

    public ResolverResultKind ResultKind => resultKind;

    public ImmutableArray<ResolverParameter> Parameters => parameters;

    public ImmutableArray<MemberBinding> Bindings => bindings;
}
