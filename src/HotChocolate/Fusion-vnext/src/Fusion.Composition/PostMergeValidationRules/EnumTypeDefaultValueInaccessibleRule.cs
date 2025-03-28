using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// This rule ensures that inaccessible enum values are not exposed in the composed schema through
/// default values. Output field arguments and input fields must only use enum values as their
/// default value when not annotated with the <c>@inaccessible</c> directive.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Enum-Type-Default-Value-Inaccessible">
/// Specification
/// </seealso>
internal sealed class EnumTypeDefaultValueInaccessibleRule
    : IEventHandler<FieldArgumentEvent>
    , IEventHandler<InputFieldEvent>
{
    public void Handle(FieldArgumentEvent @event, CompositionContext context)
    {
        var (argument, field, type, schema) = @event;

        if (type.HasFusionInaccessibleDirective()
            || field.HasFusionInaccessibleDirective()
            || argument.HasFusionInaccessibleDirective())
        {
            return;
        }

        if (argument.DefaultValue is { } defaultValue
            && !ValidateDefaultValue(defaultValue, argument.Type, out var inaccessibleCoordinate))
        {
            context.Log.Write(
                EnumTypeDefaultValueInaccessible(
                    new SchemaCoordinate(type.Name, field.Name, argument.Name),
                    inaccessibleCoordinate!.Value,
                    argument,
                    schema));
        }
    }

    public void Handle(InputFieldEvent @event, CompositionContext context)
    {
        var (inputField, inputType, schema) = @event;

        if (inputType.HasFusionInaccessibleDirective()
            || inputField.HasFusionInaccessibleDirective())
        {
            return;
        }

        if (inputField.DefaultValue is { } defaultValue
            && !ValidateDefaultValue(defaultValue, inputField.Type, out var inaccessibleCoordinate))
        {
            context.Log.Write(
                EnumTypeDefaultValueInaccessible(
                    new SchemaCoordinate(inputType.Name, inputField.Name),
                    inaccessibleCoordinate!.Value,
                    inputField,
                    schema));
        }
    }

    private static bool ValidateDefaultValue(
        IValueNode defaultValue,
        IType defaultType,
        out SchemaCoordinate? inaccessibleCoordinate)
    {
        inaccessibleCoordinate = null;

        switch (defaultValue)
        {
            case EnumValueNode enumValue:
                var enumType = (MutableEnumTypeDefinition)defaultType;

                if (!enumType.Values.TryGetValue(enumValue.Value, out var value)
                    || value.HasFusionInaccessibleDirective())
                {
                    inaccessibleCoordinate = new SchemaCoordinate(enumType.Name, enumValue.Value);

                    return false;
                }

                return true;

            case ListValueNode listValue:
                var listType = (ListType)defaultType;

                foreach (var item in listValue.Items)
                {
                    defaultType = listType.ElementType;

                    if (!ValidateDefaultValue(item, defaultType, out inaccessibleCoordinate))
                    {
                        return false;
                    }
                }

                return true;

            case ObjectValueNode objectValue:
                var inputObjectType = (MutableInputObjectTypeDefinition)defaultType;

                foreach (var field in objectValue.Fields)
                {
                    defaultType = inputObjectType.Fields[field.Name.Value].Type;

                    if (!ValidateDefaultValue(field.Value, defaultType, out inaccessibleCoordinate))
                    {
                        return false;
                    }
                }

                return true;

            default:
                return true;
        }
    }
}
