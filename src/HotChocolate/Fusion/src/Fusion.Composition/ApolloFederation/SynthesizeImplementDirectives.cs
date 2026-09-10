using HotChocolate.Fusion.Extensions;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Apollo Federation has no <c>@implement</c> marker: an implementing type simply redeclares a
/// field that an <c>@interfaceObject</c> stand-in also contributes, relying on <c>@shareable</c>
/// to make the field resolvable from both the implementing type's schema and the stand-in's
/// schema. The Composite Schema Spec instead requires such a colliding declaration to carry
/// <c>@implement</c>. This transform bridges the two models by stamping <c>@implement</c> onto a
/// shareable field that collides with a stand-in default, so a valid Apollo subgraph composes
/// without an <c>INTERFACE_OBJECT_FIELD_REQUIRES_IMPLEMENT</c> error. A non-shareable collision is
/// left untouched so the native rule still rejects it, mirroring Apollo's own field-sharing
/// rejection.
/// </summary>
internal static class SynthesizeImplementDirectives
{
    /// <summary>
    /// Applies <c>@implement</c> to shareable fields of <paramref name="schema"/> that collide with
    /// a default contributed by an <c>@interfaceObject</c> stand-in in any source schema.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    /// <param name="schemas">
    /// All mutable source schema definitions in the composition.
    /// </param>
    public static void Apply(
        MutableSchemaDefinition schema,
        IEnumerable<MutableSchemaDefinition> schemas)
    {
        foreach (var objectType in schema.Types.OfType<MutableObjectTypeDefinition>())
        {
            // A stand-in's own fields are the defaults; they never replace one.
            if (objectType.Directives.ContainsName(WellKnownDirectiveNames.InterfaceObject))
            {
                continue;
            }

            var defaultFieldNames = CollectDefaultFieldNames(objectType, schemas);

            if (defaultFieldNames.Count == 0)
            {
                continue;
            }

            foreach (var field in objectType.Fields)
            {
                if (!defaultFieldNames.Contains(field.Name)
                    || field.Directives.ContainsName(WellKnownDirectiveNames.Implement)
                    || field.Directives.ContainsName(WellKnownDirectiveNames.External))
                {
                    continue;
                }

                // Only a shareable redeclaration is a valid Apollo composition. A non-shareable
                // collision is left for the native rule to reject, matching Apollo's rejection.
                if (field.Directives.ContainsName(WellKnownDirectiveNames.Shareable))
                {
                    field.ApplyImplementDirective();
                }
            }
        }
    }

    // The default fields an interface's stand-ins contribute: non-key, non-internal,
    // non-inaccessible fields declared on any stand-in of the same name. This mirrors
    // InterfaceObjectMetadata.DefaultFields but reads directives directly, because the feature-backed
    // field metadata it relies on is only populated after preprocessing.
    private static HashSet<string> CollectDefaultFieldNames(
        MutableObjectTypeDefinition objectType,
        IEnumerable<MutableSchemaDefinition> schemas)
    {
        var schemaList = schemas as IReadOnlyCollection<MutableSchemaDefinition> ?? schemas.ToArray();
        var defaultFieldNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var interfaceType in objectType.Implements)
        {
            foreach (var (standIn, _) in InterfaceObjectMetadata.GetStandIns(schemaList, interfaceType.Name))
            {
                var keyFieldNames = InterfaceObjectMetadata.GetKeyFieldNames(standIn);

                foreach (var field in standIn.Fields)
                {
                    if (!keyFieldNames.Contains(field.Name)
                        && !field.Directives.ContainsName(WellKnownDirectiveNames.Internal)
                        && !field.Directives.ContainsName(WellKnownDirectiveNames.Inaccessible))
                    {
                        defaultFieldNames.Add(field.Name);
                    }
                }
            }
        }

        return defaultFieldNames;
    }
}
