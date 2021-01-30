using System;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Neo4J
{
    public class Neo4JNodeAttribute : ObjectTypeDescriptorAttribute
    {
        private string[] _labels;
        public Neo4JNodeAttribute(params string[] labels)
        {
            _labels = labels;
        }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectTypeDescriptor descriptor,
            Type type)
        {
            descriptor
                .Extend()
                .OnBeforeCreate(x => x.ContextData.Add(nameof(Neo4JNodeAttribute), _labels));
        }
    }
}
