using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// <code>
/// directive @authenticated on
///     ENUM
///   | FIELD_DEFINITION
///   | INTERFACE
///   | OBJECT
///   | SCALAR
/// </code>
///
/// The @authenticated directive is used to indicate that the target element is accessible only to the
/// authenticated supergraph users. For more granular access control, see the
/// <see cref="RequiresScopesDirectiveType">RequiresScopeDirectiveType</see> directive usage.
/// Refer to the <see href="https://www.apollographql.com/docs/router/configuration/authorization#authenticated">
/// Apollo Router article</see> for additional details.
/// 
/// <example>
/// type Foo @key(fields: "id") {
///   id: ID
///   description: String @authenticated
/// }
/// </example>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Enum |
    AttributeTargets.Interface |
    AttributeTargets.Method |
    AttributeTargets.Property |
    AttributeTargets.Struct)]
public sealed class AuthenticatedAttribute : DescriptorAttribute
{
    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IEnumTypeDescriptor enumTypeDescriptor:
            {
                enumTypeDescriptor.ApolloAuthenticated();
                break;
            }
            case IObjectTypeDescriptor objectFieldDescriptor:
            {
                objectFieldDescriptor.ApolloAuthenticated();
                break;
            }
            case IObjectFieldDescriptor objectFieldDescriptor:
            {
                objectFieldDescriptor.ApolloAuthenticated();
                break;
            }
            case IInterfaceTypeDescriptor interfaceTypeDescriptor:
            {
                interfaceTypeDescriptor.ApolloAuthenticated();
                break;
            }
            case IInterfaceFieldDescriptor interfaceFieldDescriptor:
            {
                interfaceFieldDescriptor.ApolloAuthenticated();
                break;
            }
        }
    }
}
