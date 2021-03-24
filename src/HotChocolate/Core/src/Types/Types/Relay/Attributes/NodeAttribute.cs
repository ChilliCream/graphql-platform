using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
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
            var nodeDescriptor = new NodeDescriptor(descriptor, type);

            descriptor.Extend().OnBeforeCreate(definition =>
            {
                // since we bind the id field late we need to hint to the type discovery
                // that we will need the ID scalar.
                definition.Dependencies.Add(
                    TypeDependency.FromSchemaType(
                        context.TypeInspector.GetType(typeof(IdType))));
            });

            descriptor.Extend().OnBeforeCompletion((descriptorContext, definition) =>
            {
                // first we try to resolve the id field.
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
                else if(context.TypeInspector.GetNodeIdMember(definition.RuntimeType) is { } sid)
                {
                    nodeDescriptor.IdField(sid);
                }

                // after that we look for the node resolver.
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
                        throw NodeAttribute_NodeResolverNotFound(type, NodeResolver);
                    }

                    nodeDescriptor.ResolveNodeWith(method);
                }
                else if (definition.RuntimeType != typeof(object) && definition.RuntimeType != type)
                {
                    MethodInfo? method = descriptorContext.TypeInspector.GetNodeResolverMethod(
                        definition.RuntimeType,
                        type);

                    if (method is null)
                    {
                        throw NodeAttribute_NodeResolverNotFound(type, NodeResolver);
                    }

                    ObjectFieldDefinition? fieldDefinition =
                        definition.Fields.FirstOrDefault(t => t.Member == method);
                    if (fieldDefinition is not null)
                    {
                        definition.Fields.Remove(fieldDefinition);
                    }

                    nodeDescriptor.ResolveNodeWith(method);
                }
                else
                {
                    nodeDescriptor.ResolveNode(type);
                }

                // we trigger a late id field configuration
                var descriptor = ObjectTypeDescriptor.From(
                    descriptorContext.DescriptorContext,
                    definition);
                nodeDescriptor.ConfigureNodeField(descriptor);
                descriptor.CreateDefinition();

                // after that we complete the type definition
                // to copy the node resolver to the context data.
                nodeDescriptor.OnCompleteDefinition(definition);
            });


        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NodeResolverAttribute : Attribute { }
}
