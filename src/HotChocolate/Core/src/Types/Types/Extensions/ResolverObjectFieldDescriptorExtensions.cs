using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types;

[Obsolete("Use Resolve(...)")]
public static class ResolverObjectFieldDescriptorExtensions
{
    // Resolver(IResolverContext)

    [Obsolete("Use Resolve(...)")]
    public static IObjectFieldDescriptor Resolver(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, object?> resolver)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        return descriptor.Resolve(ctx => new ValueTask<object?>(resolver(ctx)));
    }

    [Obsolete("Use Resolve(...)")]
    public static IObjectFieldDescriptor Resolver(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, Task<object?>> resolver)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        return descriptor.Resolve(async ctx =>
        {
            var resolverTask = resolver(ctx);
            if (resolverTask is null)
            {
                return default;
            }
            return await resolverTask.ConfigureAwait(false);
        });
    }

    [Obsolete("Use Resolve(...)")]
    public static IObjectFieldDescriptor Resolver<TResult>(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, TResult> resolver)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        return descriptor
            .Type<NativeType<TResult>>()
            .Resolve(
                ctx => new ValueTask<object?>(resolver(ctx)),
                typeof(NativeType<TResult>));
    }

    [Obsolete("Use Resolve(...)")]
    public static IObjectFieldDescriptor Resolver<TResult>(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, Task<TResult>> resolver)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        return descriptor.Resolve(
            async ctx =>
            {
                var resolverTask = resolver(ctx);
                if (resolverTask is null)
                {
                    return default;
                }
                return await resolverTask.ConfigureAwait(false);
            },
            typeof(NativeType<TResult>));
    }

    // Resolver()

    [Obsolete("Use Resolve(...)")]
    public static IObjectFieldDescriptor Resolver(
        this IObjectFieldDescriptor descriptor,
        Func<object?> resolver)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        return descriptor.Resolve(_ => new ValueTask<object?>(resolver()));
    }

    [Obsolete("Use Resolve(...)")]
    public static IObjectFieldDescriptor Resolver(
        this IObjectFieldDescriptor descriptor,
        Func<Task<object?>> resolver)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        return descriptor.Resolve(async _ => await resolver().ConfigureAwait(false));
    }

    [Obsolete("Use Resolve(...)")]
    public static IObjectFieldDescriptor Resolver<TResult>(
        this IObjectFieldDescriptor descriptor,
        Func<TResult> resolver)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        return descriptor.Resolve(_ =>
            new ValueTask<object?>(resolver()),
            typeof(NativeType<TResult>));
    }

    [Obsolete("Use Resolve(...)")]
    public static IObjectFieldDescriptor Resolver<TResult>(
        this IObjectFieldDescriptor descriptor,
        Func<Task<TResult>> resolver)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        return descriptor.Resolve(
            async _ =>
            {
                var resolverTask = resolver();
                if (resolverTask is null)
                {
                    return default;
                }
                return await resolverTask.ConfigureAwait(false);
            },
            typeof(NativeType<TResult>));
    }

    // Resolver(IResolverContext, CancellationToken)

    [Obsolete("Use Resolve(...)")]
    public static IObjectFieldDescriptor Resolver(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, CancellationToken, object?> resolver)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        return descriptor.Resolve(
            ctx => new ValueTask<object?>(resolver(ctx, ctx.RequestAborted)));
    }

    [Obsolete("Use Resolve(...)")]
    public static IObjectFieldDescriptor Resolver<TResult>(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, CancellationToken, TResult> resolver)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        return descriptor.Resolve(
            ctx => new ValueTask<object?>(resolver(ctx, ctx.RequestAborted)),
            typeof(NativeType<TResult>));
    }

    [Obsolete("Use Resolve(...)")]
    public static IObjectFieldDescriptor Resolver<TResult>(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, CancellationToken, Task<TResult>> resolver)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        return descriptor.Resolve(
             async ctx =>
            {
                var resolverTask = resolver(ctx, ctx.RequestAborted);
                if (resolverTask is null)
                {
                    return default;
                }
                return await resolverTask.ConfigureAwait(false);
            },
            typeof(NativeType<TResult>));
    }

    // Constant

    [Obsolete("Use Resolve(...)")]
    public static IObjectFieldDescriptor Resolver(
        this IObjectFieldDescriptor descriptor,
        object constantResult)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Resolve(
            _ => new ValueTask<object?>(constantResult));
    }

    [Obsolete("Use Resolve(...)")]
    public static IObjectFieldDescriptor Resolver<TResult>(
        this IObjectFieldDescriptor descriptor,
        TResult constantResult)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Resolve(
            _ => new ValueTask<object?>(constantResult),
            typeof(NativeType<TResult>));
    }
}
