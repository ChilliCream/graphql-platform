using System;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types.Relay
{
    public class NodeAttribute : ObjectTypeDescriptorAttribute
    {
        public string? IdField { get; set; }

        public string? NodeResolver { get; set; }

        public Type? NodeResolverType { get; set; }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectTypeDescriptor descriptor,
            Type type)
        {
            descriptor.ImplementsNode();

        }
    }
}
