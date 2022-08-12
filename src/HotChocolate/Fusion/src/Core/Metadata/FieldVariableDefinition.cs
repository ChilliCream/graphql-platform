using HotChocolate.Language;

namespace HotChocolate.Fusion.Metadata;

internal sealed class FieldVariableDefinition : IVariableDefinition
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

    // TODO : this probably should be a selection set ...
    public FieldNode Select { get; }
}
