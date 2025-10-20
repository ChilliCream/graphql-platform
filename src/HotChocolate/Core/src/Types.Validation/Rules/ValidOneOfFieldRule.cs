using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// If the Input Object is a OneOf Input Object then:
/// <list type="bullet">
/// <item>The type of the input field must be nullable.</item>
/// <item>The input field must not have a default value.</item>
/// </list>
/// </summary>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation">
/// Specification
/// </seealso>
public sealed class ValidOneOfFieldRule : IValidationEventHandler<InputFieldEvent>
{
    /// <summary>
    /// Checks that the OneOf input field is nullable and has no default value.
    /// </summary>
    public void Handle(InputFieldEvent @event, ValidationContext context)
    {
        var inputField = @event.InputField;

        if (inputField.DeclaringMember is IInputObjectTypeDefinition { IsOneOf: true }
            && (!inputField.Type.IsNullableType() || inputField.DefaultValue is not null))
        {
            context.Log.Write(InvalidOneOfField(inputField));
        }
    }
}
