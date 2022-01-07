using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

internal readonly struct ArgumentInfo
{
    public ArgumentInfo(NameString name, ITypeNode type, SchemaCoordinate binding)
    {
        Name = name;
        Type = type;
        Binding = binding;
    }

    public NameString Name { get; }

    public ITypeNode Type { get; }

    public SchemaCoordinate Binding { get; }
}
