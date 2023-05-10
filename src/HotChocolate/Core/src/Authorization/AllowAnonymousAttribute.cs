using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Authorization;

/// <summary>
/// Allows anonymous access to the annotated field.
/// </summary>
public sealed class AllowAnonymousAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
        => descriptor.AllowAnonymous();
}
