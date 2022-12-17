using HotChocolate.Language;

namespace HotChocolate.Fusion.Metadata;

internal interface IVariableDefinition
{
    string Name { get; }

    ITypeNode Type { get; }
}
