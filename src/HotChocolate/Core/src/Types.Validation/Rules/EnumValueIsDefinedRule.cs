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
    : IValidationEventHandler<DefaultValueNodeEvent>
    , IValidationEventHandler<DirectiveArgumentAssignmentEvent>
{
    /// <summary>
    /// Checks that enum values used in default values are defined in the enum type,
    /// at any depth within the default value.
    /// </summary>
    public void Handle(DefaultValueNodeEvent @event, ValidationContext context)
    {
        var (value, type, _, root) = @event;

        // Only the enum position itself is checked; a wrapping list is validated at its element event.
        if (type.NullableType().Kind is TypeKind.Enum
            && value is EnumValueNode enumValue
            && type.NamedType() is IEnumTypeDefinition enumType
            && !enumType.Values.ContainsName(enumValue.Value))
        {
            var entry = root.DeclaringMember is IInputObjectTypeDefinition
                ? UndefinedInputFieldDefaultEnumValue(enumValue.Value, root, enumType.Name)
                : UndefinedArgumentDefaultEnumValue(enumValue.Value, root, enumType.Name);

            context.Log.Write(entry);
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
