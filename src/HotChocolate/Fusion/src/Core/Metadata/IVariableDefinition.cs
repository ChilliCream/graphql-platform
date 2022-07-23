using HotChocolate.Language;

namespace HotChocolate.Fusion.Metadata;

public interface IVariableDefinition
{
    string Name { get; }

    ITypeNode Type { get; }
}
