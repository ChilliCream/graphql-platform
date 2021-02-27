using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Relay.Descriptors;
using static HotChocolate.Utilities.ThrowHelper;

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
            INodeDescriptor nodeDescriptor = new NodeDescriptor(descriptor, type);

            if (IdField is not null)
            {
                MemberInfo? idField = type.GetMember(IdField).FirstOrDefault();

                if (idField is null)
                {
                    throw NodeAttribute_IdFieldNotFound(type, IdField);
                }

                nodeDescriptor.IdField(idField);
            }
            else if(context.TypeInspector.GetNodeIdMember(type) is { } id)
            {
                nodeDescriptor.IdField(id);
            }

            if (NodeResolverType is not null)
            {
                if (NodeResolver is not null)
                {
                    MethodInfo? method = NodeResolverType.GetMethod(NodeResolver);

                    if (method is null)
                    {
                        throw NodeAttribute_NodeResolverNotFound(
                            NodeResolverType,
                            NodeResolver);
                    }

                    nodeDescriptor.ResolveNodeWith(method);
                }
                else
                {
                    nodeDescriptor.ResolveNodeWith(NodeResolverType);
                }
            }
            else if (NodeResolver is not null)
            {
                MethodInfo? method = type.GetMethod(NodeResolver);

                if (method is null)
                {
                    throw NodeAttribute_NodeResolverNotFound(
                        type,
                        NodeResolver);
                }

                nodeDescriptor.ResolveNodeWith(method);
            }
            else
            {
                nodeDescriptor.ResolveNode(type);
            }
        }
    }
}
