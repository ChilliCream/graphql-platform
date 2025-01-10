using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// When using the <c>@require</c> directive, the <c>fields</c> argument must always be a string
/// that defines a (potentially nested) selection set of fields from the same type. If the
/// <c>fields</c> argument is provided as a type other than a string (such as an integer, boolean,
/// or enum), the directive usage is invalid and will cause schema composition to fail.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Require-Invalid-Fields-Type">
/// Specification
/// </seealso>
internal sealed class RequireInvalidFieldsTypeRule : IEventHandler<RequireFieldsInvalidTypeEvent>
{
    public void Handle(RequireFieldsInvalidTypeEvent @event, CompositionContext context)
    {
        var (requireDirective, argument, field, type, schema) = @event;

        context.Log.Write(
            RequireInvalidFieldsType(
                requireDirective,
                argument.Name,
                field.Name,
                type.Name,
                schema));
    }
}
