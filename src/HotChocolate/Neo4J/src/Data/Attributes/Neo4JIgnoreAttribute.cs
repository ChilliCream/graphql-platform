using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Neo4J
{
    /// <summary>
    /// If this is used on a field it will be ignored.
    /// </summary>
    public class Neo4JIgnoreAttribute
        : ObjectFieldDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor
                .Extend()
                .OnBeforeCreate(x =>
                    x.ContextData.Add(nameof(Neo4JIgnoreAttribute), null));
        }
    }
}
