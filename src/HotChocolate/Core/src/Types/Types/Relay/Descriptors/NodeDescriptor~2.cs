using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static HotChocolate.Types.Relay.NodeResolverCompilerHelper;

#nullable enable

namespace HotChocolate.Types.Relay.Descriptors;

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

    public IObjectFieldDescriptor NodeResolver(NodeResolverDelegate<TNode, TId> nodeResolver) =>
        ResolveNode(nodeResolver);

    public IObjectFieldDescriptor ResolveNode(FieldResolverDelegate fieldResolver)
    {
        Definition.Resolver = fieldResolver ??
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

                typeConverter ??= ctx.GetTypeConverter();
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
            var resolver =
                Context.ResolverCompiler.CompileResolve(
                    m,
                    typeof(object),
                    typeof(TResolver),
                    ParameterExpressionBuilders);
            return ResolveNode(resolver.Resolver!);
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

        var resolver =
            Context.ResolverCompiler.CompileResolve(
                method,
                typeof(object),
                method.DeclaringType ?? typeof(object),
                ParameterExpressionBuilders);
        return ResolveNode(resolver.Resolver!);
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
