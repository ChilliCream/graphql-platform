using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// Applies the @inaccessible directive to the type system member to prevent it
/// from being accessible through the client-facing composite schema,
/// even if it is accessible in the underlying source schemas.
/// </para>
/// <para>
/// <code language="graphql">
/// type User {
///   id: ID!
///   name: String!
///   email: String! @inaccessible
/// }
/// </code>
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

            case IEnumValueDescriptor enumValueDescriptor:
                enumValueDescriptor.Inaccessible();
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

            case IObjectTypeDescriptor objectFieldDescriptor:
                objectFieldDescriptor.Inaccessible();
                break;

            case IObjectFieldDescriptor objectFieldDescriptor:
                objectFieldDescriptor.Inaccessible();
                break;

            case IArgumentDescriptor argumentDescriptor:
                argumentDescriptor.Inaccessible();
                break;

            case IUnionTypeDescriptor unionTypeDescriptor:
                unionTypeDescriptor.Inaccessible();
                break;

            default:
                throw new NotSupportedException(
                    $"The {descriptor.GetType().Name} descriptor is not supported.");
        }
    }
}
