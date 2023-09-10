using System;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Types;

public static class ScopedServiceObjectFieldDescriptorExtensions
{
    public static IObjectFieldDescriptor UseScopedService<TService>(
        this IObjectFieldDescriptor descriptor,
        Func<IServiceProvider, TService> create,
        Action<IServiceProvider, TService>? dispose = null,
        Func<IServiceProvider, TService, ValueTask>? disposeAsync = null)
    {
        var scopedServiceName = typeof(TService).FullName ?? typeof(TService).Name;

        return descriptor.Use(next => async context =>
        {
            var services = context.Service<IServiceProvider>();
            var scopedService = create(services);

            try
            {
                context.SetLocalState(scopedServiceName, scopedService);
                await next(context).ConfigureAwait(false);
                ;
            }
            finally
            {
                context.RemoveLocalState(scopedServiceName);

                dispose?.Invoke(services, scopedService);

                if (disposeAsync is { })
                {
                    await disposeAsync(services, scopedService).ConfigureAwait(false);
                }
            }
        });
    }
}
