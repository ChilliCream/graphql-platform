using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

/// <summary>
/// This rule enforces that the root query type in any source schema must be named <c>Query</c>.
/// Defining a root query type with a name other than <c>Query</c> or using a differently named type
/// alongside a type explicitly named <c>Query</c> creates inconsistencies in schema design and
/// violates the composite schema specification.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Root-Query-Used">
/// Specification
/// </seealso>
internal sealed class RootQueryUsedRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var schema = @event.Schema;
        var rootQuery = schema.QueryType;

        if (rootQuery is not null)
        {
            if (rootQuery.Name != WellKnownTypeNames.Query)
            {
                context.Log.Write(RootQueryUsed(schema));
            }
        }
        else
        {
            var namedQueryType =
                schema.Types.FirstOrDefault(t => t.Name == WellKnownTypeNames.Query);

            if (namedQueryType is not null)
            {
                context.Log.Write(RootQueryUsed(schema));
            }
        }
    }
}
