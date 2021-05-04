using System.Reflection;
using ConferencePlanner.GraphQL.Data;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace ConferencePlanner.GraphQL
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