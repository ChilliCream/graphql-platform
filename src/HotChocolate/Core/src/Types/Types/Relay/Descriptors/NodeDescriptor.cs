using System.Reflection;
using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;

#nullable enable

namespace HotChocolate.Types.Relay.Descriptors;

/// <summary>
/// The node descriptor allows to configure a node type.
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
            .OnBeforeCompletion(OnCompleteDefinition);

        Definition.NodeType = nodeType;
    }

    internal void OnCompleteDefinition(
        ITypeCompletionContext context,
        ObjectTypeDefinition definition)
        => CompleteResolver(context, definition);

    internal void ConfigureNodeField(IObjectTypeDescriptor typeDescriptor)
    {
        if (Definition.IdMember is null)
        {
            var descriptor = typeDescriptor
                .Field(NodeType.Names.Id)
                .Type<NonNullType<IdType>>();

            ConverterHelper.TryAdd(descriptor);
        }
        else
        {
            var descriptor = typeDescriptor
                .Field(Definition.IdMember)
                .Name(NodeType.Names.Id)
                .Type<NonNullType<IdType>>();

            ConverterHelper.TryAdd(descriptor);
        }
    }

    protected override IObjectFieldDescriptor ConfigureNodeField()
    {
        if (Definition.IdMember is null)
        {
            var descriptor = _typeDescriptor
                .Field(NodeType.Names.Id)
                .Type<NonNullType<IdType>>();

            return ConverterHelper.TryAdd(descriptor);
        }
        else
        {
            var descriptor = _typeDescriptor
                .Field(Definition.IdMember)
                .Name(NodeType.Names.Id)
                .Type<NonNullType<IdType>>();

            return ConverterHelper.TryAdd(descriptor);
        }
    }

    /// <inheritdoc cref="INodeDescriptor.IdField(MemberInfo)"/>
    public INodeDescriptor IdField(MemberInfo propertyOrMethod)
    {
        if (propertyOrMethod is null)
        {
            throw new ArgumentNullException(nameof(propertyOrMethod));
        }

        if (propertyOrMethod is PropertyInfo or MethodInfo)
        {
            Definition.IdMember = propertyOrMethod;
            return this;
        }

        throw new ArgumentException(NodeDescriptor_IdField_MustBePropertyOrMethod);
    }

    /// <inheritdoc cref="INodeDescriptor.ResolveNode(Type)"/>
    public IObjectFieldDescriptor ResolveNode(Type type)
        => ResolveNodeWith(
            Context.TypeInspector.GetNodeResolverMethod(
                Definition.NodeType ?? type)!);

    internal void TryResolveNode(Type type)
    {
        var resolver = Context.TypeInspector.GetNodeResolverMethod(Definition.NodeType ?? type);

        if (resolver is not null)
        {
            ResolveNodeWith(resolver);
        }
    }

    /// <inheritdoc cref="INodeDescriptor.ResolveNodeWith{TResolver}()"/>
    public IObjectFieldDescriptor ResolveNodeWith<TResolver>()
        => ResolveNodeWith(
            Context.TypeInspector.GetNodeResolverMethod(
                Definition.NodeType ?? typeof(TResolver),
                typeof(TResolver))!);

    /// <inheritdoc
    ///   cref="INodeDescriptor.ResolveNodeWith{TResolver}(Expression{Func{TResolver,object?}})"/>
    public IObjectFieldDescriptor ResolveNodeWith(Type type)
        => ResolveNodeWith(
            Context.TypeInspector.GetNodeResolverMethod(
                Definition.NodeType ?? type,
                type)!);
}
