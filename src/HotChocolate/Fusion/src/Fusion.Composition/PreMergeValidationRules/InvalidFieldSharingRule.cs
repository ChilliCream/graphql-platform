using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// <para>
/// A field in a federated GraphQL schema may be marked <c>@shareable</c>, indicating that the same
/// field can be resolved by multiple schemas without conflict. When a field is <b>not</b> marked as
/// <c>@shareable</c> (sometimes called “non-shareable”), it cannot be provided by more than one
/// schema.
/// </para>
/// <para>
/// Field definitions marked as <c>@external</c> and overridden fields are excluded when validating
/// whether a field is shareable. These annotations indicate specific cases where field ownership
/// lies with another schema or has been replaced.
/// </para>
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Invalid-Field-Sharing">
/// Specification
/// </seealso>
internal sealed class InvalidFieldSharingRule : IEventHandler<ObjectFieldGroupEvent>
{
    public void Handle(ObjectFieldGroupEvent @event, CompositionContext context)
    {
        var fieldGroup = @event.FieldGroup;

        // Exclude external and overridden fields.
        var filteredFieldGroup =
            fieldGroup
                .Where(i => i.Field is { IsExternal: false, IsOverridden: false })
                .ToImmutableArray();

        if (filteredFieldGroup.Length < 2)
        {
            return;
        }

        // Remaining fields must be shareable.
        foreach (var (field, _, schema) in filteredFieldGroup)
        {
            if (!field.IsShareable)
            {
                context.Log.Write(InvalidFieldSharing(field, schema));
            }
        }
    }
}
