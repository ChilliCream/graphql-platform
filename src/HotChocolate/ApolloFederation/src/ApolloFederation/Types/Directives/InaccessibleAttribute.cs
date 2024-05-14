using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @inaccessible on FIELD_DEFINITION
///  | OBJECT
///  | INTERFACE
///  | UNION
///  | ENUM
///  | ENUM_VALUE
///  | SCALAR
///  | INPUT_OBJECT
///  | INPUT_FIELD_DEFINITION
///  | ARGUMENT_DEFINITION
/// </code>
///
/// The @inaccessible directive is used to mark location within schema as inaccessible
/// from the GraphQL Router. Applying @inaccessible directive on a type is equivalent of applying
/// it on all type fields.
///
/// While @inaccessible fields are not exposed by the router to the clients, they are still available
/// for query plans and can be referenced from @key and @requires directives. This allows you to not
/// expose sensitive fields to your clients but still make them available for computations.
/// Inaccessible can also be used to incrementally add schema elements (e.g. fields) to multiple
/// subgraphs without breaking composition.
///
/// <example>
/// type Foo @inaccessible {
///   hiddenId: ID!
///   hiddenField: String
/// }
/// </example>
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
