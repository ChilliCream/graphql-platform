using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// <para>
/// This rule ensures that the composed schema includes at least one accessible field on the root
/// <c>Query</c> type.
/// </para>
/// <para>
/// In GraphQL, the <c>Query</c> type is essential as it defines the entry points for read
/// operations. If none of the composed schemas expose any query fields, the composed schema would
/// lack a root query, making it an invalid GraphQL schema.
/// </para>
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-No-Queries">
/// Specification
/// </seealso>
internal sealed class NoQueriesRule : IEventHandler<ObjectTypeEvent>
{
    public void Handle(ObjectTypeEvent @event, CompositionContext context)
    {
        var (objectType, schema) = @event;

        if (objectType != schema.QueryType)
        {
            return;
        }

        var accessibleFields =
            objectType.Fields.AsEnumerable().Where(f => !f.HasFusionInaccessibleDirective());

        if (!accessibleFields.Any())
        {
            context.Log.Write(NoQueries(objectType, schema));
        }
    }
}
