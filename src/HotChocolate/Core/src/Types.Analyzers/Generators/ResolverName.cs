using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Generators;

public readonly struct ResolverName(string typeName, string memberName)
{
    public readonly string TypeName = typeName;

    public readonly string MemberName = memberName;
}

public readonly struct ResolverInfo(ResolverName resolverName, IMethodSymbol? methodSymbol)
{
    public readonly ResolverName Name = resolverName;

    public readonly int ParameterCount = methodSymbol?.Parameters.Length ?? 0;

    public readonly bool Skip =>
        ParameterCount == 0 ||
        (ParameterCount == 1 && (methodSymbol?.Parameters[0]?.IsParent() ?? false));
}
