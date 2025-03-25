using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// The <c>@key</c> directive designates the fields used to identify a particular object uniquely.
/// The <c>fields</c> argument accepts a <b>string</b> that represents a selection set (for example,
/// <c>"id"</c>, or <c>"id otherField"</c>). If the <c>fields</c> argument is provided as any
/// non-string type (e.g., <c>Boolean</c>, <c>Int</c>, <c>Array</c>), the schema fails to compose
/// correctly because it cannot parse a valid field selection.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Key-Invalid-Fields-Type">
/// Specification
/// </seealso>
internal sealed class KeyInvalidFieldsTypeRule : IEventHandler<KeyFieldsInvalidTypeEvent>
{
    public void Handle(KeyFieldsInvalidTypeEvent @event, CompositionContext context)
    {
        var (keyDirective, type, schema) = @event;

        context.Log.Write(KeyInvalidFieldsType(keyDirective, type.Name, schema));
    }
}
