using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

/// <summary>
/// Specifies that the annotated resolver shall resolve services from the resolver service scope.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class UseResolverScopeAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
        => descriptor.UseResolverScope();
}
