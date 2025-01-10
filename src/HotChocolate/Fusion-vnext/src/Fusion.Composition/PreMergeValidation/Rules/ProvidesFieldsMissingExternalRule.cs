using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// <para>
/// The <c>@provides</c> directive indicates that an object type field will supply additional fields
/// belonging to the return type in this execution-specific path. Any field listed in the
/// <c>@provides(fields: ...)</c> argument must therefore be <i>external</i> in the local schema,
/// meaning that the local schema itself does <b>not</b> provide it.
/// </para>
/// <para>
/// This rule disallows selecting non-external fields in a <c>@provides</c> selection set. If a
/// field is already provided by the same schema in all execution paths, there is no need to
/// <c>@provide</c>.
/// </para>
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Provides-Fields-Missing-External">
/// Specification
/// </seealso>
internal sealed class ProvidesFieldsMissingExternalRule : IEventHandler<ProvidesFieldEvent>
{
    public void Handle(ProvidesFieldEvent @event, CompositionContext context)
    {
        var (providedField, providedType, providesDirective, field, type, schema) = @event;

        if (!providedField.HasExternalDirective())
        {
            context.Log.Write(
                ProvidesFieldsMissingExternal(
                    providedField.Name,
                    providedType.Name,
                    providesDirective,
                    field.Name,
                    type.Name,
                    schema));
        }
    }
}
