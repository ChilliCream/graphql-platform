using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// In a composed schema, fields and arguments must only reference types that are exposed. This
/// requirement guarantees that public types do not reference inaccessible structures which are
/// intended for internal use.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Reference-To-Inaccessible-Type">
/// Specification
/// </seealso>
internal sealed class ReferenceToInaccessibleTypeRule
    : IEventHandler<InputFieldEvent>
    , IEventHandler<OutputFieldEvent>
    , IEventHandler<FieldArgumentEvent>
{
    public void Handle(InputFieldEvent @event, CompositionContext context)
    {
        var (inputField, type, schema) = @event;

        if (inputField.HasFusionInaccessibleDirective())
        {
            return;
        }

        var fieldType = inputField.Type.AsTypeDefinition();

        if (fieldType.HasFusionInaccessibleDirective())
        {
            context.Log.Write(
                ReferenceToInaccessibleTypeFromInputField(
                    inputField,
                    type.Name,
                    fieldType.Name,
                    schema));
        }
    }

    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        if (field.HasFusionInaccessibleDirective())
        {
            return;
        }

        var fieldType = field.Type.AsTypeDefinition();

        if (fieldType.HasFusionInaccessibleDirective())
        {
            context.Log.Write(
                ReferenceToInaccessibleTypeFromOutputField(
                    field,
                    type.Name,
                    fieldType.Name,
                    schema));
        }
    }

    public void Handle(FieldArgumentEvent @event, CompositionContext context)
    {
        var (argument, field, _, schema) = @event;

        if (argument.HasFusionInaccessibleDirective())
        {
            return;
        }

        var argumentType = argument.Type.AsTypeDefinition();

        if (argumentType.HasFusionInaccessibleDirective())
        {
            context.Log.Write(
                ReferenceToInaccessibleTypeFromFieldArgument(
                    argument,
                    field,
                    argumentType.Name,
                    schema));
        }
    }
}
