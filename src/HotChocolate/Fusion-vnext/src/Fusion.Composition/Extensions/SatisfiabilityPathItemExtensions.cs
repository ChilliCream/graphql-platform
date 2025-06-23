using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Validators;
using HotChocolate.Types.Mutable;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.Extensions;

internal static class SatisfiabilityPathItemExtensions
{
    /// <summary>
    /// Determines if the <see cref="SatisfiabilityPathItem"/> provides the given field on the given
    /// type and schema.
    /// </summary>
    public static bool Provides(
        this SatisfiabilityPathItem item,
        MutableOutputFieldDefinition field,
        MutableObjectTypeDefinition type,
        string schemaName,
        MutableSchemaDefinition schema)
    {
        if (item.SchemaName != schemaName)
        {
            return false;
        }

        var selectionSetText = item.Field.GetFusionFieldProvides(item.SchemaName);

        if (selectionSetText is null)
        {
            return false;
        }

        var selectionSet = ParseSelectionSet($"{{ {selectionSetText} }}");
        var validator = new FieldInSelectionSetValidator(schema);

        return validator.Validate(selectionSet, item.FieldType, field, type);
    }
}
