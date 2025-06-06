#nullable enable

using HotChocolate.Resolvers;

namespace HotChocolate.Types;

public static class ResolveInterfaceFieldDescriptorExtensions
{
    // Resolve(IResolverContext)

    public static IInterfaceFieldDescriptor Resolve(
        this IInterfaceFieldDescriptor descriptor,
        Func<IResolverContext, object?> resolver)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(resolver);

        return descriptor.Resolve(ctx => new ValueTask<object?>(resolver(ctx)));
    }

    public static IInterfaceFieldDescriptor Resolve(
        this IInterfaceFieldDescriptor descriptor,
        Func<IResolverContext, Task<object?>?> resolver)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(resolver);

        return descriptor.Resolve(async ctx =>
        {
            var resolverTask = resolver(ctx);

            if (resolverTask is null)
            {
                return null;
            }

            return await resolverTask.ConfigureAwait(false);
        });
    }

    public static IInterfaceFieldDescriptor Resolve<TResult>(
        this IInterfaceFieldDescriptor descriptor,
        Func<IResolverContext, TResult> resolver)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(resolver);

        return descriptor
            .Type<NativeType<TResult>>()
            .Resolve(ctx => new ValueTask<object?>(resolver(ctx)), typeof(NativeType<TResult>));
    }

    public static IInterfaceFieldDescriptor Resolve<TResult>(
        this IInterfaceFieldDescriptor descriptor,
        Func<IResolverContext, Task<TResult>?> resolver)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(resolver);

        return descriptor.Resolve(
            async ctx =>
            {
                var resolverTask = resolver(ctx);

                if (resolverTask is null)
                {
                    return null;
                }

                return await resolverTask.ConfigureAwait(false);
            },
            typeof(NativeType<TResult>));
    }

    // Resolve()

    public static IInterfaceFieldDescriptor Resolve(
        this IInterfaceFieldDescriptor descriptor,
        Func<object?> resolver)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(resolver);

        return descriptor.Resolve(_ => new ValueTask<object?>(resolver()));
    }

    public static IInterfaceFieldDescriptor Resolve(
        this IInterfaceFieldDescriptor descriptor,
        Func<Task<object?>> resolver)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(resolver);

        return descriptor.Resolve(async _ => await resolver().ConfigureAwait(false));
    }

    public static IInterfaceFieldDescriptor Resolve<TResult>(
        this IInterfaceFieldDescriptor descriptor,
        Func<TResult> resolver)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(resolver);

        return descriptor.Resolve(
            _ => new ValueTask<object?>(resolver()),
            typeof(NativeType<TResult>));
    }

    public static IInterfaceFieldDescriptor Resolve<TResult>(
        this IInterfaceFieldDescriptor descriptor,
        Func<Task<TResult>?> resolver)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(resolver);

        return descriptor.Resolve(
            async _ =>
            {
                var resolverTask = resolver();

                if (resolverTask is null)
                {
                    return null;
                }

                return await resolverTask.ConfigureAwait(false);
            },
            typeof(NativeType<TResult>));
    }

    // Resolve(IResolverContext, CancellationToken)

    public static IInterfaceFieldDescriptor Resolve(
        this IInterfaceFieldDescriptor descriptor,
        Func<IResolverContext, CancellationToken, object?> resolver)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(resolver);

        return descriptor.Resolve(
            ctx => new ValueTask<object?>(resolver(ctx, ctx.RequestAborted)));
    }

    public static IInterfaceFieldDescriptor Resolve<TResult>(
        this IInterfaceFieldDescriptor descriptor,
        Func<IResolverContext, CancellationToken, TResult> resolver)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(resolver);

        return descriptor.Resolve(
            ctx => new ValueTask<object?>(resolver(ctx, ctx.RequestAborted)),
            typeof(NativeType<TResult>));
    }

    public static IInterfaceFieldDescriptor Resolve<TResult>(
        this IInterfaceFieldDescriptor descriptor,
        Func<IResolverContext, CancellationToken, Task<TResult>?> resolver)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(resolver);

        return descriptor.Resolve(
            async ctx =>
            {
                var resolverTask = resolver(ctx, ctx.RequestAborted);
                if (resolverTask is null)
                {
                    return null;
                }

                return await resolverTask.ConfigureAwait(false);
            },
            typeof(NativeType<TResult>));
    }

    // Constant

    public static IInterfaceFieldDescriptor Resolve(
        this IInterfaceFieldDescriptor descriptor,
        object constantResult)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Resolve(
            _ => new ValueTask<object?>(constantResult));
    }

    public static IInterfaceFieldDescriptor Resolve<TResult>(
        this IInterfaceFieldDescriptor descriptor,
        TResult constantResult)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Resolve(
            _ => new ValueTask<object?>(constantResult),
            typeof(NativeType<TResult>));
    }
}
