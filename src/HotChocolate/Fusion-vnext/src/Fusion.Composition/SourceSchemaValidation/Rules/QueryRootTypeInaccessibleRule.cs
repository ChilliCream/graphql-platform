using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

/// <summary>
/// Every source schema that contributes to the final composite schema must expose a public
/// (accessible) root query type. Marking the root query type as <c>@inaccessible</c> makes it
/// invisible to the gateway, defeating its purpose as the primary entry point for queries and
/// lookups.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Query-Root-Type-Inaccessible">
/// Specification
/// </seealso>
internal sealed class QueryRootTypeInaccessibleRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var schema = @event.Schema;
        var rootQuery = schema.QueryType;

        if (rootQuery?.HasInaccessibleDirective() == true)
        {
            context.Log.Write(QueryRootTypeInaccessible(rootQuery, schema));
        }
    }
}
