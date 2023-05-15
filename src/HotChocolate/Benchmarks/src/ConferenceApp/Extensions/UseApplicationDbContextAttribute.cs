using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.ConferencePlanner.Data;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ConferencePlanner
{
    public class UseApplicationDbContextAttribute : ObjectFieldDescriptorAttribute
    {
        public UseApplicationDbContextAttribute([CallerLineNumber] int order = 0)
        {
            Order = order;
        }

        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.UseDbContext<ApplicationDbContext>();
        }
    }
}
