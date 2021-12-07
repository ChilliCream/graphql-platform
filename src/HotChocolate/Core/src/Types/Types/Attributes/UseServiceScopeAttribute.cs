using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

/// <summary>
/// Wraps a middleware around the field that creates a service scope
/// for the wrapped pipeline.
///
/// Middleware order matters, so in most cases this should be the most outer middleware.
/// </summary>
public sealed class UseServiceScopeAttribute : ObjectFieldDescriptorAttribute
{
    public UseServiceScopeAttribute([CallerLineNumber] int order = 0)
    {
        Order = order;
    }

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
        => descriptor.UseServiceScope();
}
