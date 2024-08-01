using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types;

public static class ResolveObjectFieldDescriptorExtensions
{
    // Resolve(IResolverContext)

    public static IObjectFieldDescriptor Resolve(
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

    public static IObjectFieldDescriptor Resolve(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, Task<object?>?> resolver)
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

    public static IObjectFieldDescriptor Resolve<TResult>(
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

    public static IObjectFieldDescriptor Resolve<TResult>(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, Task<TResult>?> resolver)
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

    // Resolve()

    public static IObjectFieldDescriptor Resolve(
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

    public static IObjectFieldDescriptor Resolve(
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

    public static IObjectFieldDescriptor Resolve<TResult>(
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

        return descriptor.Resolve(
            _ => new ValueTask<object?>(resolver()),
            typeof(NativeType<TResult>));
    }

    public static IObjectFieldDescriptor Resolve<TResult>(
        this IObjectFieldDescriptor descriptor,
        Func<Task<TResult>?> resolver)
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

    // Resolve(IResolverContext, CancellationToken)

    public static IObjectFieldDescriptor Resolve(
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

    public static IObjectFieldDescriptor Resolve<TResult>(
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

    public static IObjectFieldDescriptor Resolve<TResult>(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, CancellationToken, Task<TResult>?> resolver)
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

    public static IObjectFieldDescriptor Resolve(
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

    public static IObjectFieldDescriptor Resolve<TResult>(
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
