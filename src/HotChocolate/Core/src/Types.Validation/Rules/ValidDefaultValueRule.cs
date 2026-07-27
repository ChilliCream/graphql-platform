using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// Argument and input field default values must be compatible with their type.
/// </summary>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Objects.Type-Validation">
/// Specification (Objects)
/// </seealso>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Interfaces.Type-Validation">
/// Specification (Interfaces)
/// </seealso>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation">
/// Specification (Input Objects)
/// </seealso>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation">
/// Specification (Directives)
/// </seealso>
public sealed class ValidDefaultValueRule : IValidationEventHandler<DefaultValueNodeEvent>
{
    /// <summary>
    /// Checks that the default value node is compatible with the expected type at its position.
    /// </summary>
    public void Handle(DefaultValueNodeEvent @event, ValidationContext context)
    {
        var (value, type, path, root) = @event;

        var isNonNull = type.Kind is TypeKind.NonNull;
        var unwrapped = type.NullableType();

        // A null literal is only valid at a nullable position, so reject it under a non-null type.
        if (value.Kind is SyntaxKind.NullValue)
        {
            if (isNonNull)
            {
                ReportIncompatibleType(context, root, path, type);
            }

            return;
        }

        // Default values are constant; a variable is never valid.
        if (value.Kind is SyntaxKind.Variable)
        {
            ReportIncompatibleType(context, root, path, type);

            return;
        }

        switch (unwrapped.Kind)
        {
            case TypeKind.List:
                // Element-level checks fire on the recursed element nodes.
                break;

            case TypeKind.InputObject:
                if (value is not ObjectValueNode inputObjectValue)
                {
                    ReportIncompatibleType(context, root, path, type);
                    break;
                }

                ValidateInputObject(
                    context,
                    root,
                    path,
                    (IInputObjectTypeDefinition)unwrapped.NamedType(),
                    inputObjectValue);
                break;

            case TypeKind.Enum:
                // Kind only. The undefined-name check is owned by EnumValueIsDefinedRule.
                if (value.Kind is not SyntaxKind.EnumValue)
                {
                    ReportIncompatibleType(context, root, path, type);
                }

                break;

            case TypeKind.Scalar:
                if (!((IScalarTypeDefinition)unwrapped.NamedType()).IsValueCompatible(value))
                {
                    ReportIncompatibleType(context, root, path, type);
                }

                break;
        }
    }

    private static void ValidateInputObject(
        ValidationContext context,
        IInputValueDefinition root,
        IReadOnlyList<object> path,
        IInputObjectTypeDefinition inputObject,
        ObjectValueNode value)
    {
        var providedNonNull = 0;

        foreach (var fieldValue in value.Fields)
        {
            if (!inputObject.Fields.ContainsName(fieldValue.Name.Value))
            {
                context.Log.Write(UnknownFieldInDefaultValue(root, path, fieldValue.Name.Value));
            }

            if (fieldValue.Value.Kind is not SyntaxKind.NullValue)
            {
                providedNonNull++;
            }
        }

        foreach (var field in inputObject.Fields)
        {
            var isProvided = false;

            foreach (var fieldValue in value.Fields)
            {
                if (fieldValue.Name.Value == field.Name)
                {
                    isProvided = true;
                    break;
                }
            }

            if (!isProvided && field.Type.IsNonNullType() && field.DefaultValue is null)
            {
                context.Log.Write(MissingRequiredFieldInDefaultValue(root, path, field.Name));
            }
        }

        if (inputObject.IsOneOf && providedNonNull != 1)
        {
            context.Log.Write(OneOfDefaultValueMustHaveExactlyOneField(root, path, inputObject.Name));
        }
    }

    private static void ReportIncompatibleType(
        ValidationContext context,
        IInputValueDefinition root,
        IReadOnlyList<object> path,
        IType type)
    {
        context.Log.Write(IncompatibleDefaultValueType(root, path, type.ToTypeNode().Print()));
    }
}
