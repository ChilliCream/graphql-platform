namespace HotChocolate.Types.Analyzers.Generators;

public readonly struct ResolverName(string typeName, string memberName, int argsCount)
{
    public readonly string TypeName = typeName;

    public readonly string MemberName = memberName;

    public readonly int ArgumentsCount = argsCount;
}
