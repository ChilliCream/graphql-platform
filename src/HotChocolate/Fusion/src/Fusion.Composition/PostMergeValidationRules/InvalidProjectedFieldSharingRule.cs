using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// A type may implement two or more unrelated interfaces whose <c>@interfaceObject</c> stand-ins each
/// contribute a default for the same field name. There is then no single most specific default. As
/// with <c>INVALID_FIELD_SHARING</c> for directly declared fields, composition fails with an
/// <c>INVALID_PROJECTED_FIELD_SHARING</c> error unless every contributing declaration is marked
/// <c>@shareable</c>.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Invalid-Projected-Field-Sharing">
/// Specification
/// </seealso>
internal sealed class InvalidProjectedFieldSharingRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var mergedSchema = @event.Schema;
        var schemas = context.SchemaDefinitions;

        // Report once per conflicting interface pair and field, not once per implementing type.
        var reported = new HashSet<(string, string, string)>();

        foreach (var type in mergedSchema.Types)
        {
            if (type is not MutableComplexTypeDefinition complexType)
            {
                continue;
            }

            List<MutableInterfaceTypeDefinition> ancestors = [.. complexType.Implements];

            if (ancestors.Count < 2)
            {
                continue;
            }

            var candidateFieldNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var ancestor in ancestors)
            {
                candidateFieldNames.UnionWith(InterfaceObjectMetadata.DefaultFields(schemas, ancestor.Name));
            }

            foreach (var fieldName in candidateFieldNames)
            {
                var contributors = MostSpecificContributors(ancestors, fieldName, schemas);

                if (contributors.Count < 2)
                {
                    continue;
                }

                if (!AllContributingDeclarationsShareable(contributors, fieldName, schemas))
                {
                    var names = contributors.Select(c => c.Name).Order(StringComparer.Ordinal).ToArray();
                    var key = (names[0], names[1], fieldName);

                    if (reported.Add(key))
                    {
                        context.Log.Write(
                            InvalidProjectedFieldSharing(
                                complexType.Name,
                                fieldName,
                                string.Join(", ", names.Select(n => $"'{n}'")),
                                mergedSchema));
                    }
                }
            }
        }
    }

    private static List<MutableInterfaceTypeDefinition> MostSpecificContributors(
        IReadOnlyList<MutableInterfaceTypeDefinition> ancestors,
        string fieldName,
        IReadOnlyList<MutableSchemaDefinition> schemas)
    {
        var contributors = ancestors
            .Where(a => InterfaceObjectMetadata.DefaultFields(schemas, a.Name).Contains(fieldName))
            .ToList();

        return contributors
            .Where(a => !contributors.Any(
                other => other.Name != a.Name && other.Implements.ContainsName(a.Name)))
            .ToList();
    }

    private static bool AllContributingDeclarationsShareable(
        List<MutableInterfaceTypeDefinition> contributors,
        string fieldName,
        IReadOnlyList<MutableSchemaDefinition> schemas)
    {
        foreach (var contributor in contributors)
        {
            foreach (var (standIn, _) in InterfaceObjectMetadata.GetStandIns(schemas, contributor.Name))
            {
                if (standIn.Fields.TryGetField(fieldName, out var field) && !field.IsShareable)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
