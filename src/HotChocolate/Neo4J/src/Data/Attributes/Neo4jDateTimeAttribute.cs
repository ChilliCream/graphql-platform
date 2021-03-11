using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Neo4J
{
    /// <summary>
    /// If this is used on a <see cref="System.DateTime"/> property - it will be serialized as a Neo4j Date object rather than a string.
    /// </summary>
    public class Neo4JDateTimeAttribute
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
                    x.ContextData.Add(nameof(Neo4JDateTimeAttribute), null));
        }
    }
}
