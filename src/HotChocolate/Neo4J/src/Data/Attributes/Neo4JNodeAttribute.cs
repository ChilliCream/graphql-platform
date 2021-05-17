using System;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Neo4J
{
    public class Neo4JNodeAttribute : ObjectTypeDescriptorAttribute
    {
        public Neo4JNodeAttribute(string key, params string[] labels)
        {
            Key = key;
            Labels = labels;
        }

        public string Key { get; }

        public string[] Labels { get; }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectTypeDescriptor descriptor,
            Type type)
        {
            descriptor
                .Extend()
                .OnBeforeCreate(x => x.ContextData.Add(nameof(Neo4JNodeAttribute), this));
        }
    }
}
