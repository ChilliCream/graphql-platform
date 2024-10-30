using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Marks a resolver as serial executable which will ensure that the execution engine
/// synchronizes resolver execution around the annotated resolver and ensures that
/// no other resolver is executed in parallel.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class SerialAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member) =>
        descriptor.Serial();
}
