using System;
using System.Reflection;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;

#nullable enable

namespace HotChocolate.Types.Relay.Descriptors;

public class NodeDescriptor
    : NodeDescriptorBase
    , INodeDescriptor
{
    private readonly IObjectTypeDescriptor _typeDescriptor;

    public NodeDescriptor(IObjectTypeDescriptor descriptor, Type? nodeType = null)
        : base(descriptor.Extend().Context)
    {
        _typeDescriptor = descriptor;

        _typeDescriptor
            .Implements<NodeType>()
            .Extend()
            .OnBeforeCreate(OnCompleteDefinition);

        Definition.NodeType = nodeType;
    }

    internal void OnCompleteDefinition(ObjectTypeDefinition definition)
    {
        if (Definition.Resolver is not null)
        {
            definition.ContextData[WellKnownContextData.NodeResolver] = Definition.Resolver;
        }
    }

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

    public IObjectFieldDescriptor NodeResolver(
        NodeResolverDelegate<object, object> nodeResolver) =>
        ResolveNode(nodeResolver);

    public IObjectFieldDescriptor NodeResolver<TId>(
        NodeResolverDelegate<object, TId> nodeResolver) =>
        ResolveNode(nodeResolver);

    public IObjectFieldDescriptor ResolveNodeWith<TResolver>() =>
        ResolveNodeWith(Context.TypeInspector.GetNodeResolverMethod(
            Definition.NodeType ?? typeof(TResolver),
            typeof(TResolver))!);

    public IObjectFieldDescriptor ResolveNodeWith(Type type) =>
        ResolveNodeWith(Context.TypeInspector.GetNodeResolverMethod(
            Definition.NodeType ?? type,
            type)!);

    public IObjectFieldDescriptor ResolveNode(Type type) =>
        ResolveNodeWith(Context.TypeInspector.GetNodeResolverMethod(
            Definition.NodeType ?? type)!);
}
