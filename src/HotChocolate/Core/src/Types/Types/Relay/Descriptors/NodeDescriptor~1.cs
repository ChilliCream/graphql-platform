using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;

#nullable enable

namespace HotChocolate.Types.Relay.Descriptors;

/// <summary>
/// The node descriptor allows configuring a node type.
/// </summary>
/// <typeparam name="TNode">
/// The node runtime type.
/// </typeparam>
public class NodeDescriptor<TNode>
    : NodeDescriptorBase
    , INodeDescriptor<TNode>
{
    private readonly IObjectTypeDescriptor<TNode> _typeDescriptor;

    /// <summary>
    /// Initializes a new instance of <see cref="NodeDescriptor{TNode}"/>.
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor from which the node descriptor is spawned off.
    /// </param>
    public NodeDescriptor(IObjectTypeDescriptor<TNode> descriptor)
        : base(descriptor.Extend().Context)
    {
        _typeDescriptor = descriptor;

        // we use the CompleteConfiguration instead of the higher level api since
        // we want to target a specific event.
        var ownerDef = _typeDescriptor.Implements<NodeType>().Extend().Configuration;

        var configuration = new OnCompleteTypeSystemConfigurationTask(
            (c, d) => OnCompleteConfiguration(c, (ObjectTypeConfiguration)d),
            ownerDef,
            ApplyConfigurationOn.AfterNaming);

        ownerDef.Tasks.Add(configuration);
    }

    private void OnCompleteConfiguration(
        ITypeCompletionContext context,
        ObjectTypeConfiguration configuration)
    {
        if (Configuration.ResolverField is null)
        {
            var resolverMethod =
                Context.TypeInspector.GetNodeResolverMethod(typeof(TNode), typeof(TNode));

            // we allow a node to not have a node resolver.
            // this opens up type interceptors bringing these in later.
            // we also introduced a validation option that makes sure that node resolvers are
            // available after the schema is completed.
            if (resolverMethod is not null)
            {
                ResolveNodeWith(resolverMethod);
            }
            else
            {
                ConfigureNodeField();
            }
        }

        CompleteResolver(context, configuration);
    }

    protected override IObjectFieldDescriptor ConfigureNodeField()
    {
        Configuration.NodeType = typeof(TNode);
        Configuration.IdMember ??= Context.TypeInspector.GetNodeIdMember(typeof(TNode));

        if (Configuration.IdMember is null)
        {
            var descriptor = _typeDescriptor
                .Field(NodeType.Names.Id)
                .Type<NonNullType<IdType>>();

            return ConverterHelper.TryAdd(descriptor);
        }
        else
        {
            var descriptor = _typeDescriptor
                .Field(Configuration.IdMember)
                .Name(NodeType.Names.Id)
                .Type<NonNullType<IdType>>();

            return ConverterHelper.TryAdd(descriptor);
        }
    }

    /// <inheritdoc cref="INodeDescriptor{TNode}.IdField{TId}"/>
    public INodeDescriptor<TNode, TId> IdField<TId>(
        Expression<Func<TNode, TId>> propertyOrMethod)
    {
        ArgumentNullException.ThrowIfNull(propertyOrMethod);

        var member = propertyOrMethod.TryExtractMember();

        if (member is MethodInfo or PropertyInfo)
        {
            Configuration.IdMember = member;
            return new NodeDescriptor<TNode, TId>(Context, Configuration, ConfigureNodeField);
        }

        throw new ArgumentException(NodeDescriptor_IdMember, nameof(member));
    }

    /// <inheritdoc cref="INodeDescriptor{TNode}.IdField"/>
    public INodeDescriptor<TNode> IdField(MemberInfo propertyOrMethod)
    {
        ArgumentNullException.ThrowIfNull(propertyOrMethod);

        if (propertyOrMethod is MethodInfo or PropertyInfo)
        {
            Configuration.IdMember = propertyOrMethod;
            return this;
        }

        throw new ArgumentException(NodeDescriptor_IdField_MustBePropertyOrMethod);
    }

    /// <inheritdoc cref="INodeDescriptor{TNode}.ResolveNode{TId}"/>
    public IObjectFieldDescriptor ResolveNode<TId>(
        NodeResolverDelegate<TNode, TId> fieldResolver)
    {
        ArgumentNullException.ThrowIfNull(fieldResolver);

        return ResolveNode(async ctx =>
        {
            if (ctx.LocalContextData.TryGetValue(WellKnownContextData.InternalId, out var o)
                && o is TId id)
            {
                return await fieldResolver(ctx, id).ConfigureAwait(false);
            }

            return null;
        });
    }

    /// <inheritdoc cref="INodeDescriptor{TNode}.ResolveNodeWith{TResolver}()"/>
    public IObjectFieldDescriptor ResolveNodeWith<TResolver>()
    {
        return ResolveNodeWith(
            Context.TypeInspector.GetNodeResolverMethod(
                typeof(TNode),
                typeof(TResolver))!);
    }

    /// <inheritdoc cref="INodeDescriptor{TNode}.ResolveNodeWith(Type)"/>
    public IObjectFieldDescriptor ResolveNodeWith(Type type) =>
        ResolveNodeWith(
            Context.TypeInspector.GetNodeResolverMethod(
                typeof(TNode),
                type)!);
}
