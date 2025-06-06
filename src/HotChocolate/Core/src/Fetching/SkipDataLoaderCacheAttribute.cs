using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Fetching;

/// <summary>
/// Disables the DataLoader caching for a root field.
/// </summary>
public sealed class SkipDataLoaderCacheAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
        => descriptor.Extend().Configuration.Flags |= CoreFieldFlags.UsesProjections;
}
