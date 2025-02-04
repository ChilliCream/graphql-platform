using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// The <c>@key</c> directive is used to define the set of fields that uniquely identify an entity.
/// These fields must not include any field that is defined with arguments, as arguments introduce
/// variability that prevents consistent and valid entity resolution across subgraphs. Fields
/// included in the <c>fields</c> argument of the <c>@key</c> directive must be static and
/// consistently resolvable.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Key-Fields-Has-Arguments">
/// Specification
/// </seealso>
internal sealed class KeyFieldsHasArgumentsRule : IEventHandler<KeyFieldEvent>
{
    public void Handle(KeyFieldEvent @event, CompositionContext context)
    {
        var (keyDirective, entityType, field, type, schema) = @event;

        if (field.Arguments.Count != 0)
        {
            context.Log.Write(
                KeyFieldsHasArguments(
                    entityType.Name,
                    keyDirective,
                    field.Name,
                    type.Name,
                    schema));
        }
    }
}
