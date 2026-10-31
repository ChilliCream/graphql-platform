using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Marks a field as not being treated as a data resolver for automatic default directives.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Method,
    Inherited = true,
    AllowMultiple = false)]
internal sealed class NotDataResolverAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
        => descriptor.Extend().Configuration.Flags |= CoreFieldFlags.NotDataResolver;
}
