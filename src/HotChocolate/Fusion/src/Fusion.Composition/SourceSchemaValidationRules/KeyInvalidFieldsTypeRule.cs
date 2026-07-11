using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
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
internal sealed class KeyInvalidFieldsTypeRule : IEventHandler<ComplexTypeEvent>
{
    public void Handle(ComplexTypeEvent @event, CompositionContext context)
    {
        var (complexType, schema) = @event;

        foreach (var (keyDirective, keyInfo) in complexType.KeyInfoByDirective)
        {
            if (keyInfo.IsInvalidFieldsType)
            {
                context.Log.Write(KeyInvalidFieldsType(keyDirective, complexType, schema));
            }
        }
    }
}
