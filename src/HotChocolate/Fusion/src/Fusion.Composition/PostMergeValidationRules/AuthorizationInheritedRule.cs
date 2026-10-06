using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Features;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// Warns about every merged member that requires authorization through interface inheritance
/// although none of its source schemas annotated it directly.
/// </summary>
internal sealed class AuthorizationInheritedRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        if (@event.Schema.Features.Get<AuthorizationInheritanceMetadata>() is not { } metadata)
        {
            return;
        }

        foreach (var entry in metadata.Entries)
        {
            context.Log.Write(
                AuthorizationInherited(entry.Coordinate, entry.Paths, entry.SourceSchemas));
        }
    }
}
