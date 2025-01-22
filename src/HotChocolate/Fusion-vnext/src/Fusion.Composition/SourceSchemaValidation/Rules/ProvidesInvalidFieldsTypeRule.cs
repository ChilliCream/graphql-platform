using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

/// <summary>
/// The <c>@provides</c> directive indicates that a field is <b>providing</b> one or more additional
/// fields on the returned (child) type. The <c>fields</c> argument accepts a <b>string</b>
/// representing a GraphQL selection set (for example, <c>"title author"</c>). If the <c>fields</c>
/// argument is given as a non-string type (e.g., <c>Boolean</c>, <c>Int</c>, <c>Array</c>), the
/// schema fails to compose because it cannot interpret a valid selection set.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Provides-Invalid-Fields-Type">
/// Specification
/// </seealso>
internal sealed class ProvidesInvalidFieldsTypeRule : IEventHandler<ProvidesFieldsInvalidTypeEvent>
{
    public void Handle(ProvidesFieldsInvalidTypeEvent @event, CompositionContext context)
    {
        var (providesDirective, field, type, schema) = @event;

        context.Log.Write(
            ProvidesInvalidFieldsType(providesDirective, field.Name, type.Name, schema));
    }
}
