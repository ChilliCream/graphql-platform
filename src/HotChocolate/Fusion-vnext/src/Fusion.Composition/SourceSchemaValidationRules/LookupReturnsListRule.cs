using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Fields annotated with the <c>@lookup</c> directive are intended to retrieve a single entity
/// based on provided arguments. To avoid ambiguity in entity resolution, such fields must return a
/// single object and not a list. This validation rule enforces that any field annotated with
/// <c>@lookup</c> must have a return type that is <b>NOT</b> a list.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Lookup-Returns-List">
/// Specification
/// </seealso>
internal sealed class LookupReturnsListRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        if (field.HasLookupDirective() && field.Type.NullableType() is ListType)
        {
            context.Log.Write(LookupReturnsList(field, type, schema));
        }
    }
}
