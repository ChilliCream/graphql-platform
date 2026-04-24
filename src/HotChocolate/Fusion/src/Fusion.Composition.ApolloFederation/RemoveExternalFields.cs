using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Removes fields marked with <c>@external</c> from complex types.
/// </summary>
internal static class RemoveExternalFields
{
    /// <summary>
    /// Removes all <c>@external</c> fields from the schema.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    public static void Apply(MutableSchemaDefinition schema)
    {
        foreach (var type in schema.Types)
        {
            if (type is not MutableComplexTypeDefinition complexType)
            {
                continue;
            }

            var externalFields = new List<MutableOutputFieldDefinition>();

            foreach (var field in complexType.Fields)
            {
                if (field.Directives.ContainsName(FederationDirectiveNames.External))
                {
                    externalFields.Add(field);
                }
            }

            foreach (var field in externalFields)
            {
                complexType.Fields.Remove(field);
            }
        }
    }
}
