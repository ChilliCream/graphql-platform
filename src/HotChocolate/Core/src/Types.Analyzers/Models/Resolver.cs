using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class Resolver(
    string typeName,
    ISymbol member,
    ResolverResultKind resultKind,
    ImmutableArray<ResolverParameter> parameters,
    bool isNodeResolver = false)
{
    public string TypeName => typeName;

    public ISymbol Member => member;

    public bool IsPure
        => !isNodeResolver
            && resultKind is ResolverResultKind.Pure
            && parameters.All(t => t.IsPure);

    public bool IsNodeResolver => isNodeResolver;

    public ResolverResultKind ResultKind => resultKind;

    public ImmutableArray<ResolverParameter> Parameters => parameters;
}
