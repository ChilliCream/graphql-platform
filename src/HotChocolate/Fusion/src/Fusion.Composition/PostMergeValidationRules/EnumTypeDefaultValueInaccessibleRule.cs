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
                    argument,
                    inaccessibleCoordinate!.Value,
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
                    inputField,
                    inaccessibleCoordinate!.Value,
                    schema));
        }
    }

    private static bool ValidateDefaultValue(
        IValueNode defaultValue,
        IType defaultType,
        out SchemaCoordinate? inaccessibleCoordinate)
    {
        inaccessibleCoordinate = null;

        // Spec list input coercion: a non-list value on a list type is treated as a singleton list.
        if (defaultType.NullableType() is ListType coercedListType
            && defaultValue.Kind is not (SyntaxKind.ListValue or SyntaxKind.NullValue or SyntaxKind.Variable))
        {
            return ValidateDefaultValue(
                defaultValue,
                coercedListType.ElementType,
                out inaccessibleCoordinate);
        }

        switch (defaultValue)
        {
            case EnumValueNode enumValue:
                if (defaultType.NullableType() is not MutableEnumTypeDefinition enumType)
                {
                    return true;
                }

                if (!enumType.Values.TryGetValue(enumValue.Value, out var value)
                    || value.HasFusionInaccessibleDirective())
                {
                    inaccessibleCoordinate = new SchemaCoordinate(enumType.Name, enumValue.Value);

                    return false;
                }

                return true;

            case ListValueNode listValue:
                if (defaultType.NullableType() is not ListType listType)
                {
                    return true;
                }

                foreach (var item in listValue.Items)
                {
                    if (!ValidateDefaultValue(item, listType.ElementType, out inaccessibleCoordinate))
                    {
                        return false;
                    }
                }

                return true;

            case ObjectValueNode objectValue:
                if (defaultType.NullableType() is not MutableInputObjectTypeDefinition inputObjectType)
                {
                    return true;
                }

                foreach (var field in objectValue.Fields)
                {
                    if (!inputObjectType.Fields.TryGetField(field.Name.Value, out var inputField))
                    {
                        continue;
                    }

                    if (!ValidateDefaultValue(field.Value, inputField.Type, out inaccessibleCoordinate))
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
