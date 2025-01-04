using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// <para>
/// This rule ensures that enum types with the same name across different source schemas in a
/// composite schema have identical sets of values. Enums must be consistent across source schemas
/// to avoid conflicts and ambiguities in the composite schema.
/// </para>
/// <para>
/// When an enum is defined with differing values, it can lead to confusion and errors in query
/// execution. For instance, a value valid in one schema might be passed to another where it’s
/// unrecognized, leading to unexpected behavior or failures. This rule prevents such
/// inconsistencies by enforcing that all instances of the same named enum across schemas have an
/// exact match in their values.
/// </para>
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Enum-Type-Values-Must-Be-The-Same-Across-Source-Schemas">
/// Specification
/// </seealso>
internal sealed class EnumValuesMustBeTheSameAcrossSchemasRule : IEventHandler<EnumTypeGroupEvent>
{
    public void Handle(EnumTypeGroupEvent @event, CompositionContext context)
    {
        var (_, enumGroup) = @event;

        if (enumGroup.Length < 2)
        {
            return;
        }

        var allValues = enumGroup
            .SelectMany(e => e.Type.Values)
            .ToImmutableArray();

        var inaccessibleValues = allValues
            .Where(ValidationHelper.IsInaccessible)
            .Select(e => e.Name)
            .ToImmutableHashSet();
        var requiredValues = allValues
            .Select(e => e.Name)
            .Except(inaccessibleValues)
            .ToImmutableHashSet();

        foreach (var @enum in enumGroup)
        {
            var enumValues = @enum.Type.Values
                .Select(e => e.Name)
                .Except(inaccessibleValues)
                .ToImmutableHashSet();
            if (enumValues.Count != requiredValues.Count)
            {
                context.Log.Write(
                    EnumValuesMustBeTheSameAcrossSchemas(@enum.Type, @enum.Schema));
            }
        }
    }
}
