using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Validators;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Even if the <c>@provides(fields: "…")</c> argument is well-formed syntactically, the selected
/// fields must actually exist on the return type of the field. Invalid field references—e.g.,
/// selecting non-existent fields, referencing fields on the wrong type, or incorrectly omitting
/// required nested selections—lead to a <c>PROVIDES_INVALID_FIELDS</c> error.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Provides-Invalid-Fields">
/// Specification
/// </seealso>
internal sealed class ProvidesInvalidFieldsRule : IEventHandler<ProvidesFieldsEvent>
{
    public void Handle(ProvidesFieldsEvent @event, CompositionContext context)
    {
        var (selectionSet, providesDirective, field, type, schema) = @event;

        var validator = new SelectionSetValidator(schema);
        var errors = validator.Validate(selectionSet, field.Type.AsTypeDefinition());

        if (errors.Any())
        {
            context.Log.Write(
                ProvidesInvalidFields(
                    providesDirective,
                    field.Name,
                    type.Name,
                    schema,
                    errors));
        }
    }
}
