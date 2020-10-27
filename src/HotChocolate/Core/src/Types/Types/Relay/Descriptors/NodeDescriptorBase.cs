using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.Expressions;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Relay.Descriptors
{
    public abstract class NodeDescriptorBase : DescriptorBase<NodeDefinition>
    {
        protected NodeDescriptorBase(IDescriptorContext context)
            : base(context)
        {
        }

        protected internal sealed override NodeDefinition Definition { get; protected set; } =
            new NodeDefinition();

        protected abstract IObjectFieldDescriptor ConfigureNodeField();

        public virtual IObjectFieldDescriptor ResolveNode(
            FieldResolverDelegate fieldResolver)
        {
            Definition.Resolver = fieldResolver ??
                throw new ArgumentNullException(nameof(fieldResolver));

            return ConfigureNodeField();
        }

        public IObjectFieldDescriptor ResolveNode<TId>(
            NodeResolverDelegate<object, TId> fieldResolver)
        {
            if (fieldResolver is null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            return ResolveNode(async ctx =>
            {
                if (ctx.LocalContextData.TryGetValue(
                    WellKnownContextData.InternalId,
                    out object? o) && o is TId id)
                {
                    return await fieldResolver(ctx, id).ConfigureAwait(false);
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

            MemberInfo member = method.TryExtractMember();

            if (member is MethodInfo m)
            {
                FieldResolver resolver =
                    ResolverCompiler.Resolve.Compile(
                        new ResolverDescriptor(
                            typeof(TResolver),
                            typeof(object),
                            new FieldMember("_", "_", m)));
                return ResolveNode(resolver.Resolver);
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

            FieldResolver resolver =
                ResolverCompiler.Resolve.Compile(
                    new ResolverDescriptor(
                        method.DeclaringType ?? typeof(object),
                        typeof(object),
                        new FieldMember("_", "_", method)));
            return ResolveNode(resolver.Resolver);
        }
    }
}
