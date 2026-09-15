using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Relay.Descriptors;

/// <summary>
/// The node descriptor allows configuring a node type.
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
        NodeConfiguration configuration,
        Func<IObjectFieldDescriptor> configureNodeField)
    {
        Context = context;
        Configuration = configuration;
        _configureNodeField = configureNodeField;
    }

    private IDescriptorContext Context { get; }

    private NodeConfiguration Configuration { get; }

    public IObjectFieldDescriptor ResolveNodeBatch(BatchResolverDelegate batchResolver)
    {
        ArgumentNullException.ThrowIfNull(batchResolver);
        Configuration.ResolverField ??= new ObjectFieldConfiguration();
        ObjectFieldDescriptor.From(Context, Configuration.ResolverField).ResolveBatch(batchResolver);
        Configuration.ResolverField.Resolver = null;
        return _configureNodeField();
    }

    public IObjectFieldDescriptor ResolveNodeBatch(BatchNodeResolverDelegate<TNode, TId> batchResolver)
    {
        ArgumentNullException.ThrowIfNull(batchResolver);
        Configuration.ResolverField ??= new ObjectFieldConfiguration();
        BatchNodeResolverHelper.Configure(Configuration.ResolverField, batchResolver);
        return _configureNodeField();
    }

    public IObjectFieldDescriptor ResolveNodeBatchWith<TResolver>(Expression<Func<TResolver, object?>> method)
    {
        ArgumentNullException.ThrowIfNull(method);
        var member = method.ExtractMember();
        var resolverField = Configuration.ResolverField ??= new ObjectFieldConfiguration();
        ObjectFieldDescriptor.From(Context, resolverField).ResolveBatchWith(method);
        resolverField.Member = member;
        resolverField.ResolverMember = null;
        resolverField.BatchResolver = null;
        return _configureNodeField();
    }

    public IObjectFieldDescriptor ResolveNodeBatchWith(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);
        var resolverField = Configuration.ResolverField ??= new ObjectFieldConfiguration();
        ObjectFieldDescriptor.From(Context, resolverField).ResolveBatchWith(method);
        resolverField.Member = method;
        resolverField.ResolverMember = null;
        resolverField.BatchResolver = null;
        return _configureNodeField();
    }

    public IObjectFieldDescriptor NodeResolver(NodeResolverDelegate<TNode, TId> nodeResolver)
        => ResolveNode(nodeResolver);

    public IObjectFieldDescriptor ResolveNode(FieldResolverDelegate fieldResolver)
    {
        Configuration.ResolverField ??= new ObjectFieldConfiguration();
        Configuration.ResolverField.Resolver = fieldResolver ??
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
        ArgumentNullException.ThrowIfNull(method);

        var member = method.TryExtractMember();

        if (member is MethodInfo m)
        {
            if (m.IsDefined(typeof(BatchResolverAttribute)))
            {
                return ResolveNodeBatchWith(method);
            }

            Configuration.ResolverField ??= new ObjectFieldConfiguration();
            Configuration.ResolverField.Member = m;
            Configuration.ResolverField.DeclaringType = m.ReflectedType ?? m.DeclaringType;
            Configuration.ResolverField.ResolverType = typeof(TResolver);
            return _configureNodeField();
        }

        throw new ArgumentException(
            TypeResources.NodeDescriptor_MustBeMethod,
            nameof(member));
    }

    public IObjectFieldDescriptor ResolveNodeWith(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);

        if (method.IsDefined(typeof(BatchResolverAttribute)))
        {
            return ResolveNodeBatchWith(method);
        }

        Configuration.ResolverField ??= new ObjectFieldConfiguration();
        Configuration.ResolverField.Member = method;
        Configuration.ResolverField.DeclaringType = method.ReflectedType ?? method.DeclaringType;
        Configuration.ResolverField.ResolverType = method.DeclaringType ?? typeof(object);
        return _configureNodeField();
    }

    public IObjectFieldDescriptor ResolveNodeWith<TResolver>()
    {
#pragma warning disable IL2087 // 'TNode'/'TResolver' does not satisfy DAM requirements
        return ResolveNodeWith(Context.TypeInspector.GetNodeResolverMethod(
            typeof(TNode),
            typeof(TResolver))!);
#pragma warning restore IL2087
    }

    public IObjectFieldDescriptor ResolveNodeWith(Type type)
    {
#pragma warning disable IL2087 // 'TNode' does not satisfy DAM requirements
#pragma warning disable IL2067 // 'type' does not satisfy DAM requirements
        return ResolveNodeWith(Context.TypeInspector.GetNodeResolverMethod(
            typeof(TNode),
            type)!);
#pragma warning restore IL2067
#pragma warning restore IL2087
    }
}
