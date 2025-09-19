using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Fields annotated with the <c>@lookup</c> directive are intended to retrieve a single entity
/// based on provided arguments. To properly handle cases where the requested entity does not exist,
/// such fields should have a nullable return type. This allows the field to return null when an
/// entity matching the provided criteria is not found, following the standard GraphQL practices for
/// representing missing data.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Lookup-Returns-Non-Nullable-Type">
/// Specification
/// </seealso>
internal sealed class LookupReturnsNonNullableTypeRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        if (field.HasLookupDirective() && field.Type is NonNullType)
        {
            context.Log.Write(LookupReturnsNonNullableType(field, type, schema));
        }
    }
}
