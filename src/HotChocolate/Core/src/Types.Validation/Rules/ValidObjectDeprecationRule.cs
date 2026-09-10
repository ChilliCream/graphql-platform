using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// A field that is not deprecated must not return a deprecated object type.
/// </summary>
public sealed class ValidObjectDeprecationRule : IValidationEventHandler<OutputFieldEvent>
{
    /// <summary>
    /// Checks that a field which is not deprecated does not return a deprecated object type.
    /// </summary>
    public void Handle(OutputFieldEvent @event, ValidationContext context)
    {
        var field = @event.OutputField;

        if (field.IsDeprecated)
        {
            return;
        }

        if (field.Type.NamedType() is IObjectTypeDefinition { IsDeprecated: true } objectType)
        {
            context.Log.Write(InvalidObjectDeprecation(field, objectType));
        }
    }
}
