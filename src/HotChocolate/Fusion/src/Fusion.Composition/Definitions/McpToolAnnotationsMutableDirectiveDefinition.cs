using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class McpToolAnnotationsMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public McpToolAnnotationsMutableDirectiveDefinition(MutableScalarTypeDefinition booleanType)
        : base(McpToolAnnotations)
    {
        Arguments.Add(new MutableInputFieldDefinition(DestructiveHint, booleanType));
        Arguments.Add(new MutableInputFieldDefinition(IdempotentHint, booleanType));
        Arguments.Add(new MutableInputFieldDefinition(OpenWorldHint, booleanType));
        Locations = DirectiveLocation.FieldDefinition;
    }

    public static McpToolAnnotationsMutableDirectiveDefinition Create(ISchemaDefinition schema)
    {
        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(SpecScalarNames.Boolean.Name, out var booleanType))
        {
            booleanType = BuiltIns.Boolean.Create();
        }

        return new McpToolAnnotationsMutableDirectiveDefinition(booleanType);
    }
}
