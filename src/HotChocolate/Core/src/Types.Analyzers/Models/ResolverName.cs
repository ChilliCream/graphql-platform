namespace HotChocolate.Types.Analyzers.Models;

public readonly struct ResolverName(string typeName, string memberName)
{
    public readonly string TypeName = typeName;

    public readonly string MemberName = memberName;
}
