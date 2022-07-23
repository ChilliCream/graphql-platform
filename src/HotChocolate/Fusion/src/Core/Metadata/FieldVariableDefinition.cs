using HotChocolate.Language;

namespace HotChocolate.Fusion.Metadata;

public sealed class FieldVariableDefinition : IVariableDefinition
{
    public FieldVariableDefinition(string name, string schemaName, ITypeNode type, FieldNode select)
    {
        Name = name;
        SchemaName = schemaName;
        Type = type;
        Select = select;
    }

    public string Name { get; }

    public string SchemaName { get; }

    public ITypeNode Type { get; }

    public FieldNode Select { get; }
}
