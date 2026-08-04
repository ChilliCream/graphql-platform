using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

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
internal sealed class ProvidesInvalidFieldsTypeRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, _, schema) = @event;

        if (field.ProvidesInfo is { IsInvalidFieldsType: true } providesInfo)
        {
            context.Log.Write(ProvidesInvalidFieldsType(providesInfo.Directive, field, schema));
        }
    }
}
