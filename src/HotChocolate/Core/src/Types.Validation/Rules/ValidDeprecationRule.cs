using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// Required arguments and Input Object fields must not be deprecated.
/// </summary>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Objects.Type-Validation">
/// Specification (Objects)
/// </seealso>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Interfaces.Type-Validation">
/// Specification (Interfaces)
/// </seealso>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation">
/// Specification (Directives)
/// </seealso>
public sealed class ValidDeprecationRule : IValidationEventHandler<InputValueEvent>
{
    /// <summary>
    /// Checks that required arguments and Input Object fields are not deprecated.
    /// </summary>
    public void Handle(InputValueEvent @event, ValidationContext context)
    {
        var inputValue = @event.InputValue;

        if (inputValue.IsDeprecated
            && inputValue.Type.IsNonNullType()
            && inputValue.DefaultValue is null)
        {
            var error = inputValue.DeclaringMember is IInputObjectTypeDefinition
                ? InvalidInputFieldDeprecation(inputValue)
                : InvalidArgumentDeprecation(inputValue);

            context.Log.Write(error);
        }
    }
}
