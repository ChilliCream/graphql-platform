using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Relay.Descriptors;
using static System.Reflection.BindingFlags;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types.Relay;

/// <summary>
/// This attribute marks a relay node type.
/// </summary>
public class NodeAttribute : ObjectTypeDescriptorAttribute
{
    public NodeAttribute()
    {
        RequiresAttributeProvider = true;
    }

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
        Type? type)
    {
        if (type is null)
        {
            return;
        }

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
#pragma warning disable IL2070
                    var idField = type.GetMember(IdField).FirstOrDefault(
                        t => t.MemberType is MemberTypes.Method or MemberTypes.Property);
#pragma warning restore IL2070

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
                typeDescriptor.CreateConfiguration();

                // invoke completion explicitly.
                nodeDescriptor.OnCompleteConfiguration(completionContext, definition);
            });

        descriptor.Extend().OnBeforeCompletion((completionContext, definition) =>
        {
            // after that we look for the node resolver.
            if (NodeResolverType is not null)
            {
                if (NodeResolver is not null)
                {
#pragma warning disable IL2075
                    var method = NodeResolverType.GetMethod(
                        NodeResolver,
                        Instance | Static | Public | FlattenHierarchy);
#pragma warning restore IL2075

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
#pragma warning disable IL2070
                var method = type.GetMethod(
                    NodeResolver,
                    Instance | Static | Public | FlattenHierarchy);
#pragma warning restore IL2070

                if (method is not null)
                {
                    nodeDescriptor.ResolveNodeWith(method);
                }
            }
            else if (definition.RuntimeType != typeof(object) && definition.RuntimeType != type)
            {
#pragma warning disable IL2072 // 'nodeType'/'resolverType' does not satisfy DAM requirements
#pragma warning disable IL2067
                var method = completionContext.TypeInspector.GetNodeResolverMethod(
                    definition.RuntimeType,
                    type);
#pragma warning restore IL2067
#pragma warning restore IL2072

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

            // invoke completion explicitly.
            nodeDescriptor.OnCompleteConfiguration(completionContext, definition);
        });
    }
}
