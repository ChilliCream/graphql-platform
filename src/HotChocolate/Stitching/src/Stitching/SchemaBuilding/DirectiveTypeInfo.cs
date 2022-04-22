using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

public class DirectiveTypeInfo : IDirectiveTypeInfo
{
    public DirectiveTypeInfo(
        DirectiveDefinitionNode definition,
        ISchemaInfo schema)
    {
        Definition = definition;
        Schema = schema;
    }

    public DirectiveDefinitionNode Definition { get; }

    public ISchemaInfo Schema { get; }
}
