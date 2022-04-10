using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

public interface IDirectiveTypeInfo
{
    DirectiveDefinitionNode Definition { get; }
    ISchemaInfo Schema { get; }
}
