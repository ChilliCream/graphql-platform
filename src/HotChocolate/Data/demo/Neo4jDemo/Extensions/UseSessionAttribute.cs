using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Neo4j.Driver;

namespace Neo4jDemo.Extensions
{
    public class UseSessionAttribute : ObjectFieldDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.UseSession<IAsyncSession>();
        }
    }
}
