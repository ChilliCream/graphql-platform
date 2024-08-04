using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class RequiredArgument(string name, ITypeNode type)
{
    public string Name { get; } = name;

    public ITypeNode Type { get; } = type;
}
