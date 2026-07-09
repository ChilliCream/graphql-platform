using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// An Input Object type must define one or more input fields.
/// </summary>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation">
/// Specification
/// </seealso>
public sealed class NonEmptyInputObjectTypeRule : IValidationEventHandler<InputObjectTypeEvent>
{
    /// <summary>
    /// Checks that the Input Object type defines one or more fields.
    /// </summary>
    public void Handle(InputObjectTypeEvent @event, ValidationContext context)
    {
        var inputObjectType = @event.InputObjectType;

        if (!inputObjectType.Fields.Any())
        {
            context.Log.Write(EmptyInputObjectType(inputObjectType));
        }
    }
}
