using HotChocolate.Language;

namespace HotChocolate.Fusion.Metadata;

internal sealed class ArgumentVariableDefinition : IVariableDefinition
{
    public ArgumentVariableDefinition(string name, ITypeNode type, string argumentName)
    {
        Name = name;
        Type = type;
        ArgumentName = argumentName;
    }

    public string Name { get; }

    public ITypeNode Type { get; }

    public string ArgumentName { get; }
}
