using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.Expressions;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Relay.Descriptors
{
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

        public IObjectFieldDescriptor NodeResolver(
            NodeResolverDelegate<TNode, TId> nodeResolver) =>
            ResolveNode(nodeResolver);

        public IObjectFieldDescriptor ResolveNode(
            FieldResolverDelegate fieldResolver)
        {
            Definition.Resolver = fieldResolver ??
                throw new ArgumentNullException(nameof(fieldResolver));

            return _configureNodeField();
        }

        public IObjectFieldDescriptor ResolveNode(
            NodeResolverDelegate<TNode, TId> fieldResolver) =>
            ResolveNode(async ctx =>
            {
                if (ctx.LocalContextData.TryGetValue(
                    WellKnownContextData.InternalId,
                    out object? o) && o is TId id)
                {
                    return await fieldResolver(ctx, id).ConfigureAwait(false);
                }

                return null;
            });

        public IObjectFieldDescriptor ResolveNodeWith<TResolver>(
            Expression<Func<TResolver, object?>> method)
        {
            if (method is null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            MemberInfo member = method.TryExtractMember();

            if (member is MethodInfo m)
            {
                FieldResolver resolver =
                    ResolverCompiler.Resolve.Compile(
                        new ResolverDescriptor(
                            typeof(object),
                            new FieldMember("_", "_", m),
                            resolverType: typeof(TResolver)));
                return ResolveNode(resolver.Resolver);
            }

            throw new ArgumentException(
                TypeResources.NodeDescriptor_MustBeMethod,
                nameof(member));
        }

        public IObjectFieldDescriptor ResolveNodeWith(
            MethodInfo method)
        {
            if (method is null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            FieldResolver resolver =
                ResolverCompiler.Resolve.Compile(
                    new ResolverDescriptor(
                        typeof(object),
                        new FieldMember("_", "_", method),
                        resolverType: method.DeclaringType ?? typeof(object)));
            return ResolveNode(resolver.Resolver);
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
}
