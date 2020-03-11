using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public static partial class ResolverObjectFieldDescriptorExtensions
    {
        // Resolver(IResolverContext)

        public static IObjectFieldDescriptor Resolver(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, object> resolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return descriptor.Resolver(ctx =>
                Task.FromResult(resolver(ctx)));
        }

        public static IObjectFieldDescriptor Resolver(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, Task<object>> resolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return descriptor.Resolver(ctx =>
                resolver(ctx));
        }

        public static IObjectFieldDescriptor Resolver<TResult>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, TResult> resolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return descriptor
                .Type<NativeType<TResult>>()
                .Resolver(ctx => Task.FromResult<object>(resolver(ctx)),
                    typeof(NativeType<TResult>));
        }

        public static IObjectFieldDescriptor Resolver<TResult>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, Task<TResult>> resolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return descriptor.Resolver(
                async ctx =>
                {
                    Task<TResult> resolverTask = resolver(ctx);
                    if (resolverTask == null)
                    {
                        return default;
                    }
                    return await resolverTask.ConfigureAwait(false);
                },
                typeof(NativeType<TResult>));
        }

        // Resolver()

        public static IObjectFieldDescriptor Resolver(
            this IObjectFieldDescriptor descriptor,
            Func<object> resolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return descriptor.Resolver(ctx =>
                Task.FromResult(resolver()));
        }

        public static IObjectFieldDescriptor Resolver(
            this IObjectFieldDescriptor descriptor,
            Func<Task<object>> resolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return descriptor.Resolver(ctx => resolver());
        }

        public static IObjectFieldDescriptor Resolver<TResult>(
            this IObjectFieldDescriptor descriptor,
            Func<TResult> resolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return descriptor.Resolver(ctx =>
                Task.FromResult<object>(resolver()),
                typeof(NativeType<TResult>));
        }

        public static IObjectFieldDescriptor Resolver<TResult>(
            this IObjectFieldDescriptor descriptor,
            Func<Task<TResult>> resolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return descriptor.Resolver(
                async ctx =>
                {
                    Task<TResult> resolverTask = resolver();
                    if (resolverTask == null)
                    {
                        return default;
                    }
                    return await resolverTask.ConfigureAwait(false);
                },
                typeof(NativeType<TResult>));
        }

        // Resolver(IResolverContext, CancellationToken)

        public static IObjectFieldDescriptor Resolver(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, CancellationToken, object> resolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return descriptor.Resolver(
                ctx => resolver(ctx, ctx.RequestAborted));
        }

        public static IObjectFieldDescriptor Resolver<TResult>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, CancellationToken, TResult> resolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return descriptor.Resolver(
                ctx => Task.FromResult<object>(
                    resolver(ctx, ctx.RequestAborted)),
                typeof(NativeType<TResult>));
        }

        public static IObjectFieldDescriptor Resolver<TResult>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, CancellationToken, Task<TResult>> resolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return descriptor.Resolver(
                 async ctx =>
                {
                    Task<TResult> resolverTask = resolver(
                        ctx, ctx.RequestAborted);
                    if (resolverTask == null)
                    {
                        return default;
                    }
                    return await resolverTask.ConfigureAwait(false);
                },
                typeof(NativeType<TResult>));
        }

        // Constant

        public static IObjectFieldDescriptor Resolver(
            this IObjectFieldDescriptor descriptor,
            object constantResult)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Resolver(
                ctx => Task.FromResult(constantResult));
        }

        public static IObjectFieldDescriptor Resolver<TResult>(
            this IObjectFieldDescriptor descriptor,
            TResult constantResult)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Resolver(
                ctx => Task.FromResult<object>(constantResult),
                typeof(NativeType<TResult>));
        }
    }
}
