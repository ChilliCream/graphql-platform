using System.Reflection;
using HotChocolate.ConferencePlanner.Data;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ConferencePlanner
{
    public class UseApplicationDbContextAttribute : ObjectFieldDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.UseDbContext<ApplicationDbContext>();
        }
    }
}