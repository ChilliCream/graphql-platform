using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// Checks that enum values used in default values and assignments are defined in the enum type.
/// </summary>
public sealed class EnumValueIsDefinedRule
    : IValidationEventHandler<ArgumentEvent>
    , IValidationEventHandler<InputFieldEvent>
    , IValidationEventHandler<DirectiveArgumentAssignmentEvent>
{
    /// <summary>
    /// Checks that enum values used in argument default values are defined in the enum type.
    /// </summary>
    public void Handle(ArgumentEvent @event, ValidationContext context)
    {
        var argument = @event.Argument;
        var argumentType = argument.Type.AsTypeDefinition();

        if (argumentType is IEnumTypeDefinition enumType
            && argument.DefaultValue is EnumValueNode enumValue
            && !enumType.Values.ContainsName(enumValue.Value))
        {
            context.Log.Write(
                UndefinedArgumentDefaultEnumValue(enumValue.Value, argument, enumType.Name));
        }
    }

    /// <summary>
    /// Checks that enum values used in input field default values are defined in the enum type.
    /// </summary>
    public void Handle(InputFieldEvent @event, ValidationContext context)
    {
        var inputField = @event.InputField;
        var fieldType = inputField.Type.AsTypeDefinition();

        if (fieldType is IEnumTypeDefinition enumType
            && inputField.DefaultValue is EnumValueNode enumValue
            && !enumType.Values.ContainsName(enumValue.Value))
        {
            context.Log.Write(
                UndefinedInputFieldDefaultEnumValue(enumValue.Value, inputField, enumType.Name));
        }
    }

    /// <summary>
    /// Checks that enum values used in directive argument assignments are defined in the enum type.
    /// </summary>
    public void Handle(DirectiveArgumentAssignmentEvent @event, ValidationContext context)
    {
        var (assignment, argument, directive, member) = @event;
        var argumentType = argument.Type.AsTypeDefinition();

        if (argumentType is IEnumTypeDefinition enumType
            && assignment.Value is EnumValueNode enumValue
            && !enumType.Values.ContainsName(enumValue.Value))
        {
            context.Log.Write(
                UndefinedArgumentAssignedEnumValue(
                    enumValue.Value,
                    argument.Name,
                    directive.Name,
                    enumType.Name,
                    member));
        }
    }
}
