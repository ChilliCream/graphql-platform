using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// When using the <c>@is</c> directive, the <c>field</c> argument must always be a string that
/// describes how the arguments can be mapped from the entity type that the lookup field resolves.
/// If the <c>field</c> argument is provided as a type other than a string (such as an integer,
/// boolean, or enum), the directive usage is invalid and will cause schema composition to fail.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Is-Invalid-Field-Type">
/// Specification
/// </seealso>
internal sealed class IsInvalidFieldTypeRule : IEventHandler<FieldArgumentEvent>
{
    public void Handle(FieldArgumentEvent @event, CompositionContext context)
    {
        var (argument, _, _, schema) = @event;

        if (argument.IsInfo is { IsInvalidFieldType: true } isInfo)
        {
            context.Log.Write(IsInvalidFieldType(isInfo.Directive, argument, schema));
        }
    }
}
