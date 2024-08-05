#nullable enable
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

/// <summary>
/// Marks a resolver as returning a stream result
/// which will allow the execution engine to compile a result handler for the resolver.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class StreamResultAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
        => descriptor.StreamResult();
}
