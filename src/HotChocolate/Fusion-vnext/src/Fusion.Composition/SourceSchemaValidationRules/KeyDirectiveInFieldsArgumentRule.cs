using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// The <c>@key</c> directive specifies the set of fields used to uniquely identify an entity. The
/// <c>fields</c> argument must consist of a valid GraphQL selection set that does not include any
/// directive applications. Directives in the <c>fields</c> argument are not supported.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Key-Directive-in-Fields-Argument">
/// Specification
/// </seealso>
internal sealed class KeyDirectiveInFieldsArgumentRule : IEventHandler<ComplexTypeEvent>
{
    public void Handle(ComplexTypeEvent @event, CompositionContext context)
    {
        var (complexType, schema) = @event;

        foreach (var (keyDirective, keyInfo) in complexType.KeyInfoByDirective)
        {
            foreach (var (fieldNode, fieldNamePath) in keyInfo.FieldNodes)
            {
                if (fieldNode.Directives.Count != 0)
                {
                    context.Log.Write(
                        KeyDirectiveInFieldsArgument(
                            complexType,
                            keyDirective,
                            fieldNamePath,
                            schema));
                }
            }
        }
    }
}
