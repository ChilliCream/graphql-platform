using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Selections
{
    public sealed class UseSingleOrDefaultAttribute
        : ObjectFieldDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.UseSingleOrDefault();
        }
    }
}
