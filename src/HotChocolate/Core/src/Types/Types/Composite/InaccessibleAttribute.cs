using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// The @inaccessible directive is used to prevent specific type system members
/// from being accessible through the client-facing composite schema,
/// even if they are accessible in the underlying source schemas.
/// </para>
/// <para>
/// This directive is useful for restricting access to type system members that
/// are either irrelevant to the client-facing composite schema or sensitive in nature,
/// such as internal identifiers or fields intended only for backend use.
/// </para>
/// <para>
/// directive @inaccessible on FIELD_DEFINITION
///   | OBJECT | INTERFACE | UNION | ARGUMENT_DEFINITION
///   | SCALAR | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--inaccessible"/>
/// </para>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Enum
    | AttributeTargets.Field
    | AttributeTargets.Interface
    | AttributeTargets.Method
    | AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Struct)]
public sealed class InaccessibleAttribute : DescriptorAttribute
{
    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IEnumTypeDescriptor enumTypeDescriptor:
                enumTypeDescriptor.Inaccessible();
                break;
            case IObjectTypeDescriptor objectFieldDescriptor:
                objectFieldDescriptor.Inaccessible();
                break;
            case IObjectFieldDescriptor objectFieldDescriptor:
                objectFieldDescriptor.Inaccessible();
                break;
            case IInterfaceTypeDescriptor interfaceTypeDescriptor:
                interfaceTypeDescriptor.Inaccessible();
                break;
            case IInterfaceFieldDescriptor interfaceFieldDescriptor:
                interfaceFieldDescriptor.Inaccessible();
                break;
            case IInputObjectTypeDescriptor inputObjectTypeDescriptor:
                inputObjectTypeDescriptor.Inaccessible();
                break;
            case IInputFieldDescriptor inputFieldDescriptor:
                inputFieldDescriptor.Inaccessible();
                break;
            case IUnionTypeDescriptor unionTypeDescriptor:
                unionTypeDescriptor.Inaccessible();
                break;
            case IEnumValueDescriptor enumValueDescriptor:
                enumValueDescriptor.Inaccessible();
                break;
        }
    }
}
