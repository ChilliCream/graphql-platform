using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;

namespace HotChocolate
{
    public static class NodeObjectTypeExtensions
    {
        public static IObjectTypeDescriptor AsNode<TNode>(
            this IObjectTypeDescriptor descriptor,
            NodeResolverDelegate<TNode> nodeResolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (nodeResolver == null)
            {
                throw new ArgumentNullException(nameof(nodeResolver));
            }

            return AsNode(descriptor,
                sp => new NodeResolver<TNode>(nodeResolver));
        }

        public static IObjectTypeDescriptor AsNode<TNode, TId>(
            this IObjectTypeDescriptor descriptor,
            NodeResolverDelegate<TNode, TId> nodeResolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (nodeResolver == null)
            {
                throw new ArgumentNullException(nameof(nodeResolver));
            }

            return AsNode(descriptor,
                sp => new NodeResolver<TNode, TId>(nodeResolver));
        }

        public static IObjectTypeDescriptor AsNode(
            this IObjectTypeDescriptor descriptor,
            INodeResolver nodeResolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (nodeResolver == null)
            {
                throw new ArgumentNullException(nameof(nodeResolver));
            }

            return AsNode(descriptor, sp => nodeResolver);
        }

        public static IObjectTypeDescriptor AsNode(
            this IObjectTypeDescriptor descriptor,
            Func<IServiceProvider, INodeResolver> nodeResolverFactory)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (nodeResolverFactory == null)
            {
                throw new ArgumentNullException(nameof(nodeResolverFactory));
            }

            descriptor
                .Interface<NodeType>()
                .Extend()
                .OnBeforeCreate(c =>
                {
                    c.ContextData[RelayConstants.NodeResolverFactory] =
                        nodeResolverFactory;
                });

            return descriptor;
        }

        public static IObjectTypeDescriptor<T> AsNode<T, TNode>(
            this IObjectTypeDescriptor<T> descriptor,
            NodeResolverDelegate<TNode> nodeResolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (nodeResolver == null)
            {
                throw new ArgumentNullException(nameof(nodeResolver));
            }

            return AsNode(descriptor,
                sp => new NodeResolver<TNode>(nodeResolver));
        }

        public static IObjectTypeDescriptor<T> AsNode<T, TNode, TId>(
            this IObjectTypeDescriptor<T> descriptor,
            NodeResolverDelegate<TNode, TId> nodeResolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (nodeResolver == null)
            {
                throw new ArgumentNullException(nameof(nodeResolver));
            }

            return AsNode(descriptor,
                sp => new NodeResolver<TNode, TId>(nodeResolver));
        }

        public static IObjectTypeDescriptor<T> AsNode<T>(
            this IObjectTypeDescriptor<T> descriptor,
            INodeResolver nodeResolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (nodeResolver == null)
            {
                throw new ArgumentNullException(nameof(nodeResolver));
            }

            return AsNode(descriptor, sp => nodeResolver);
        }

        public static IObjectTypeDescriptor<T> AsNode<T>(
            this IObjectTypeDescriptor<T> descriptor,
            Func<IServiceProvider, INodeResolver> nodeResolverFactory)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (nodeResolverFactory == null)
            {
                throw new ArgumentNullException(nameof(nodeResolverFactory));
            }

            descriptor
                .Interface<NodeType>()
                .Extend()
                .OnBeforeCreate(c =>
                {
                    c.ContextData[RelayConstants.NodeResolverFactory] =
                        nodeResolverFactory;
                });

            return descriptor;
        }
    }
}
