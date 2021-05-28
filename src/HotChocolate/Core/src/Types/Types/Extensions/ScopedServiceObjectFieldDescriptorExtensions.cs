using System;
using System.Threading.Tasks;
using HotChocolate;

#nullable enable

namespace HotChocolate.Types
{
    public static class ScopedServiceObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor UseScopedService<TService>(
            this IObjectFieldDescriptor descriptor,
            Func<IServiceProvider, TService> create,
            Action<IServiceProvider, TService>? dispose = null,
            Func<IServiceProvider, TService, ValueTask>? disposeAsync = null)
        {
            string scopedServiceName = typeof(TService).FullName ?? typeof(TService).Name;

            return descriptor.Use(next => async context =>
            {
                IServiceProvider services = context.Service<IServiceProvider>();
                TService scopedService = create(services);

                try
                {
                    context.SetLocalValue(scopedServiceName, scopedService);
                    await next(context).ConfigureAwait(false); ;
                }
                finally
                {
                    context.RemoveLocalValue(scopedServiceName);

                    dispose?.Invoke(services, scopedService);

                    if (disposeAsync is { })
                    {

                        await disposeAsync(services, scopedService).ConfigureAwait(false);
                    }
                }
            });
        }
    }
}