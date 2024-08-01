using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Relay.Descriptors;

/// <summary>
/// The node descriptor allows to configure a node type.
/// </summary>
/// <typeparam name="TNode">
/// The node runtime type.
/// </typeparam>
/// <typeparam name="TId">
/// The node id runtime type.
/// </typeparam>
public class NodeDescriptor<TNode, TId> : INodeDescriptor<TNode, TId>
{
    private readonly Func<IObjectFieldDescriptor> _configureNodeField;

    public NodeDescriptor(
        IDescriptorContext context,
        NodeDefinition definition,
        Func<IObjectFieldDescriptor> configureNodeField)
    {
        Context = context;
        Definition = definition;
        _configureNodeField = configureNodeField;
    }

    private IDescriptorContext Context { get; }

    private NodeDefinition Definition { get; }

    public IObjectFieldDescriptor NodeResolver(NodeResolverDelegate<TNode, TId> nodeResolver)
        => ResolveNode(nodeResolver);

    public IObjectFieldDescriptor ResolveNode(FieldResolverDelegate fieldResolver)
    {
        Definition.ResolverField ??= new ObjectFieldDefinition();
        Definition.ResolverField.Resolver = fieldResolver ??
            throw new ArgumentNullException(nameof(fieldResolver));

        return _configureNodeField();
    }

    public IObjectFieldDescriptor ResolveNode(NodeResolverDelegate<TNode, TId> fieldResolver)
    {
        ITypeConverter? typeConverter = null;

        return ResolveNode(async ctx =>
        {
            if (ctx.LocalContextData.TryGetValue(WellKnownContextData.InternalId, out var id))
            {
                if (id is TId c)
                {
                    return await fieldResolver(ctx, c).ConfigureAwait(false);
                }

                typeConverter ??= ctx.Services.GetService<ITypeConverter>() ?? DefaultTypeConverter.Default;
                c = typeConverter.Convert<object, TId>(id);
                return await fieldResolver(ctx, c).ConfigureAwait(false);
            }

            return null;
        });
    }

    public IObjectFieldDescriptor ResolveNodeWith<TResolver>(
        Expression<Func<TResolver, object?>> method)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        var member = method.TryExtractMember();

        if (member is MethodInfo m)
        {
            Definition.ResolverField ??= new ObjectFieldDefinition();
            Definition.ResolverField.Member = m;
            Definition.ResolverField.ResolverType = typeof(TResolver);
            return _configureNodeField();
        }

        throw new ArgumentException(
            TypeResources.NodeDescriptor_MustBeMethod,
            nameof(member));
    }

    public IObjectFieldDescriptor ResolveNodeWith(MethodInfo method)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        Definition.ResolverField ??= new ObjectFieldDefinition();
        Definition.ResolverField.Member = method;
        Definition.ResolverField.ResolverType = method.DeclaringType ?? typeof(object);
        return _configureNodeField();
    }

    public IObjectFieldDescriptor ResolveNodeWith<TResolver>() =>
        ResolveNodeWith(Context.TypeInspector.GetNodeResolverMethod(
            typeof(TNode),
            typeof(TResolver))!);

    public IObjectFieldDescriptor ResolveNodeWith(Type type) =>
        ResolveNodeWith(Context.TypeInspector.GetNodeResolverMethod(
            typeof(TNode),
            type)!);
}
