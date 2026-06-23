using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Validators;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

internal sealed class SubscribeMessageInvalidFieldsRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, _, schema) = @event;
        var validator = new SelectionSetValidator(schema);

        foreach (var directive in field.GetSubscribeDirectives())
        {
            var errors = validator.Validate(directive.Message, field.Type.AsTypeDefinition());

            if (errors.Any())
            {
                context.Log.Write(SubscribeMessageInvalidFields(field, schema, errors));
            }
        }
    }
}
