using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types
{
    public sealed class UseSelectionAttribute : ObjectFieldDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.UseSelection();
        }
    }
}
