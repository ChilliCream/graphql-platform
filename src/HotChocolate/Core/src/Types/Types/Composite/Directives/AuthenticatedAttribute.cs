using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Composite;

/// <summary>
/// Applies the @authenticated directive to the annotated member.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Interface
    | AttributeTargets.Enum
    | AttributeTargets.Method
    | AttributeTargets.Property,
    AllowMultiple = false)]
public sealed class AuthenticatedAttribute : DescriptorAttribute
{
    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider)
    {
        switch (descriptor)
        {
            case IObjectTypeDescriptor desc:
                desc.Authenticated();
                break;

            case IObjectFieldDescriptor desc:
                desc.Authenticated();
                break;

            case IInterfaceTypeDescriptor desc:
                desc.Authenticated();
                break;

            case IInterfaceFieldDescriptor desc:
                desc.Authenticated();
                break;

            case IEnumTypeDescriptor desc:
                desc.Authenticated();
                break;

            case IScalarTypeDescriptor desc:
                desc.Authenticated();
                break;

            default:
                throw ThrowHelper.AuthorizationDirective_UnsupportedDescriptor(
                    DirectiveNames.Authenticated.Name,
                    attributeProvider,
                    descriptor);
        }
    }
}
