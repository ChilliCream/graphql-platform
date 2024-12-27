using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// This rule enforces that, for any source schema, if a root mutation type is defined, it must be
/// named <c>Mutation</c>. Defining a root mutation type with a name other than <c>Mutation</c> or
/// using a differently named type alongside a type explicitly named <c>Mutation</c> creates
/// inconsistencies in schema design and violates the composite schema specification.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Root-Mutation-Used">
/// Specification
/// </seealso>
internal sealed class RootMutationUsedRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var schema = @event.Schema;
        var rootMutation = schema.MutationType;

        if (rootMutation is not null)
        {
            if (rootMutation.Name != WellKnownTypeNames.Mutation)
            {
                context.Log.Write(RootMutationUsed(schema));
            }
        }
        else
        {
            var namedMutationType =
                schema.Types.FirstOrDefault(t => t.Name == WellKnownTypeNames.Mutation);

            if (namedMutationType is not null)
            {
                context.Log.Write(RootMutationUsed(schema));
            }
        }
    }
}
