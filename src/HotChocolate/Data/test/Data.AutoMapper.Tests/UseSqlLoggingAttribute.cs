using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections;

public class UseSqlLoggingAttribute : ObjectFieldDescriptorAttribute
{
    public override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
        => descriptor.UseSqlLogging();
}
