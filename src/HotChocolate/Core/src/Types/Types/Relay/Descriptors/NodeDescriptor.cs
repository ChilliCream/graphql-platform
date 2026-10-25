using System.Reflection;
using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types.Relay.Descriptors;

/// <summary>
/// The node descriptor allows configuring a node type.
/// </summary>
public class NodeDescriptor
    : NodeDescriptorBase
    , INodeDescriptor
{
    private readonly IObjectTypeDescriptor _typeDescriptor;

    /// <summary>
    /// Initializes a new instance of <see cref="NodeDescriptor"/>.
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor from which the node descriptor is spawned off.
    /// </param>
    /// <param name="nodeType">
    /// The node type.
    /// </param>
    public NodeDescriptor(IObjectTypeDescriptor descriptor, Type? nodeType = null)
        : base(descriptor.Extend().Context)
    {
        _typeDescriptor = descriptor;

        _typeDescriptor
            .Implements<NodeType>()
            .Extend()
            .OnBeforeCompletion(OnCompleteConfiguration);

        Configuration.NodeType = nodeType;
    }

    internal void OnCompleteConfiguration(
        ITypeCompletionContext context,
        ObjectTypeConfiguration configuration)
        => CompleteResolver(context, configuration);

    internal void ConfigureNodeField(IObjectTypeDescriptor typeDescriptor)
    {
        if (Configuration.IdMember is null)
        {
            var descriptor = typeDescriptor
                .Field(NodeType.Names.Id)
                .Type<NonNullType<IdType>>();

            ConverterHelper.TryAdd(descriptor);
        }
        else
        {
            var descriptor = typeDescriptor
                .Field(Configuration.IdMember)
                .Name(NodeType.Names.Id)
                .Type<NonNullType<IdType>>();

            ConverterHelper.TryAdd(descriptor);
        }
    }

    protected override IObjectFieldDescriptor ConfigureNodeField()
    {
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

    /// <inheritdoc cref="INodeDescriptor.IdField(MemberInfo)"/>
    public INodeDescriptor IdField(MemberInfo propertyOrMethod)
    {
        ArgumentNullException.ThrowIfNull(propertyOrMethod);

        if (propertyOrMethod is PropertyInfo or MethodInfo)
        {
            Configuration.IdMember = propertyOrMethod;
            return this;
        }

        throw new ArgumentException(NodeDescriptor_IdField_MustBePropertyOrMethod);
    }

    /// <inheritdoc cref="INodeDescriptor.ResolveNode(Type)"/>
    public IObjectFieldDescriptor ResolveNode(Type type)
    {
#pragma warning disable IL2072 // 'nodeType' does not satisfy DAM requirements
#pragma warning disable IL2067 // 'resolverType' does not satisfy DAM requirements
        return ResolveNodeWith(
            Context.TypeInspector.GetNodeResolverMethod(
                Configuration.NodeType ?? type)!);
#pragma warning restore IL2067
#pragma warning restore IL2072
    }

    internal void TryResolveNode(Type type)
    {
#pragma warning disable IL2072 // 'nodeType' does not satisfy DAM requirements
#pragma warning disable IL2067 // 'resolverType' does not satisfy DAM requirements
        var resolver = Context.TypeInspector.GetNodeResolverMethod(Configuration.NodeType ?? type);
#pragma warning restore IL2067
#pragma warning restore IL2072

        if (resolver is not null)
        {
            ResolveNodeWith(resolver);
        }
    }

    /// <inheritdoc cref="INodeDescriptor.ResolveNodeWith{TResolver}()"/>
    public IObjectFieldDescriptor ResolveNodeWith<TResolver>()
    {
#pragma warning disable IL2072 // 'nodeType' does not satisfy DAM requirements
#pragma warning disable IL2087 // 'resolverType' does not satisfy DAM requirements
        return ResolveNodeWith(
            Context.TypeInspector.GetNodeResolverMethod(
                Configuration.NodeType ?? typeof(TResolver),
                typeof(TResolver))!);
#pragma warning restore IL2087
#pragma warning restore IL2072
    }

    /// <inheritdoc
    ///   cref="INodeDescriptor.ResolveNodeWith{TResolver}(Expression{Func{TResolver,object?}})"/>
    public IObjectFieldDescriptor ResolveNodeWith(Type type)
    {
#pragma warning disable IL2072 // 'nodeType' does not satisfy DAM requirements
#pragma warning disable IL2067 // 'resolverType' does not satisfy DAM requirements
        return ResolveNodeWith(
            Context.TypeInspector.GetNodeResolverMethod(
                Configuration.NodeType ?? type,
                type)!);
#pragma warning restore IL2067
#pragma warning restore IL2072
    }
}
