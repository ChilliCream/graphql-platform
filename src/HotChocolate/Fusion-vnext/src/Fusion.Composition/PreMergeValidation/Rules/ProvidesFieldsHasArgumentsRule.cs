using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// The <c>@provides</c> directive specifies fields that a resolver provides for the parent type.
/// The <c>fields</c> argument must reference fields that do not have arguments, as fields with
/// arguments introduce variability that is incompatible with the consistent behavior expected of
/// <c>@provides</c>.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Provides-Fields-Has-Arguments">
/// Specification
/// </seealso>
internal sealed class ProvidesFieldsHasArgumentsRule : IEventHandler<ProvidesFieldEvent>
{
    public void Handle(ProvidesFieldEvent @event, CompositionContext context)
    {
        var (providedField, providedType, providesDirective, field, type, schema) = @event;

        if (providedField.Arguments.Count != 0)
        {
            context.Log.Write(
                ProvidesFieldsHasArguments(
                    providedField.Name,
                    providedType.Name,
                    providesDirective,
                    field.Name,
                    type.Name,
                    schema));
        }
    }
}
