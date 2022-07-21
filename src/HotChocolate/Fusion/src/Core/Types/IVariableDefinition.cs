using HotChocolate.Language;

namespace HotChocolate.Fusion.Types;

public interface IVariableDefinition
{
    string Name { get; }

    ITypeNode Type { get; }
}
