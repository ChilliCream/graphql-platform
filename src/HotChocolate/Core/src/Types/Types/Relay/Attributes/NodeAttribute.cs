#nullable enable

using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Relay.Descriptors;
using static System.Reflection.BindingFlags;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types.Relay;

/// <summary>
/// This attribute marks a relay node type.
/// </summary>
public class NodeAttribute : ObjectTypeDescriptorAttribute
{
    /// <summary>
    /// The name of the member representing the ID field of the node.
    /// </summary>
    public string? IdField { get; set; }

    /// <summary>
    /// The name of the member representing the node resolver.
    /// </summary>
    public string? NodeResolver { get; set; }

    /// <summary>
    /// The type of the node resolver.
    /// </summary>
    public Type? NodeResolverType { get; set; }

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type type)
    {
        var nodeDescriptor = new NodeDescriptor(descriptor, type);

        descriptor.Extend().OnBeforeCreate(
            definition =>
            {
                // since we bind the id field late we need to hint to the type discovery
                // that we will need the ID scalar.
                definition.Dependencies.Add(
                    TypeDependency.FromSchemaType(
                        context.TypeInspector.GetType(typeof(IdType))));
            });

        descriptor.Extend().OnBeforeNaming(
            (completionContext, definition) =>
            {
                // first we try to resolve the id field.
                if (IdField is not null)
                {
                    var idField = type.GetMember(IdField).FirstOrDefault(
                        t => t.MemberType is MemberTypes.Method or MemberTypes.Property);

                    if (idField is null)
                    {
                        throw NodeAttribute_IdFieldNotFound(type, IdField);
                    }

                    nodeDescriptor.IdField(idField);
                }
                else if (context.TypeInspector.GetNodeIdMember(type) is { } id)
                {
                    nodeDescriptor.IdField(id);
                }
                else if (context.TypeInspector.GetNodeIdMember(definition.RuntimeType) is { } sid)
                {
                    nodeDescriptor.IdField(sid);
                }

                // we trigger a late id field configuration
                var typeDescriptor = ObjectTypeDescriptor.From(
                    completionContext.DescriptorContext,
                    definition);
                nodeDescriptor.ConfigureNodeField(typeDescriptor);
                typeDescriptor.CreateDefinition();

                // invoke completion explicitly.
                nodeDescriptor.OnCompleteDefinition(completionContext, definition);
            });

        descriptor.Extend().OnBeforeCompletion((completionContext, definition) =>
        {
            // after that we look for the node resolver.
            if (NodeResolverType is not null)
            {
                if (NodeResolver is not null)
                {
                    var method = NodeResolverType.GetMethod(
                        NodeResolver,
                        Instance | Static | Public | FlattenHierarchy);

                    if (method is not null)
                    {
                        nodeDescriptor.ResolveNodeWith(method);
                    }
                }
                else
                {
                    nodeDescriptor.ResolveNodeWith(NodeResolverType);
                }
            }
            else if (NodeResolver is not null)
            {
                var method = type.GetMethod(
                    NodeResolver,
                    Instance | Static | Public | FlattenHierarchy);

                if (method is not null)
                {
                    nodeDescriptor.ResolveNodeWith(method);
                }
            }
            else if (definition.RuntimeType != typeof(object) && definition.RuntimeType != type)
            {
                var method = completionContext.TypeInspector.GetNodeResolverMethod(
                    definition.RuntimeType,
                    type);

                if (method is not null)
                {
                    if (definition.Fields.Any(
                        t => t.Member == method || t.ResolverMember == method))
                    {
                        foreach (var fieldDefinition in definition.Fields
                            .Where(t => t.Member == method || t.ResolverMember == method)
                            .ToArray())
                        {
                            definition.Fields.Remove(fieldDefinition);
                        }
                    }

                    nodeDescriptor.ResolveNodeWith(method);
                }
            }
            else
            {
                nodeDescriptor.TryResolveNode(type);
            }

            // we trigger a late id field configuration
            var typeDescriptor = ObjectTypeDescriptor.From(
                completionContext.DescriptorContext,
                definition);
            nodeDescriptor.ConfigureNodeField(typeDescriptor);
            typeDescriptor.CreateDefinition();

            // invoke completion explicitly.
            nodeDescriptor.OnCompleteDefinition(completionContext, definition);
        });
    }
}
