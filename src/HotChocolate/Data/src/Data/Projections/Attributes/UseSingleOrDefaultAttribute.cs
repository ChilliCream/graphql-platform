using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data
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
