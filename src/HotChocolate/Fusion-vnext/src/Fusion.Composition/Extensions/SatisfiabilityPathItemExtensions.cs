using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Validators;
using HotChocolate.Types.Mutable;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.Extensions;

internal static class SatisfiabilityPathItemExtensions
{
    /// <summary>
    /// Determines if the <see cref="ISatisfiabilityPathItem"/> provides the given field on the given
    /// type and schema.
    /// </summary>
    public static bool Provides(
        this ISatisfiabilityPathItem item,
        MutableOutputFieldDefinition field,
        MutableObjectTypeDefinition type,
        string schemaName,
        MutableSchemaDefinition schema)
    {
        if (item is not SatisfiabilityPathItem pathItem)
        {
            return false;
        }

        if (pathItem.SchemaName != schemaName)
        {
            return false;
        }

        var selectionSetText = pathItem.Field.GetFusionFieldProvides(pathItem.SchemaName);

        if (selectionSetText is null)
        {
            return false;
        }

        var selectionSet = ParseSelectionSet($"{{ {selectionSetText} }}");
        var validator = new FieldInSelectionSetValidator(schema);

        return validator.Validate(selectionSet, pathItem.FieldType, field, type);
    }
}
