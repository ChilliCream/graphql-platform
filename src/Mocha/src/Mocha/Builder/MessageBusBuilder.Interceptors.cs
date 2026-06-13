using Microsoft.Extensions.DependencyInjection;

namespace Mocha;

public partial class MessageBusBuilder
{
    private readonly List<BusInterceptorRegistration> _busInterceptorRegistrations = [];

    /// <inheritdoc />
    public IMessageBusBuilder AddBusInterceptor<T>() where T : BusInterceptor
    {
        _busInterceptorRegistrations.Add(new BusInterceptorRegistration
        {
            InterceptorType = typeof(T)
        });

        return this;
    }

    /// <inheritdoc />
    public IMessageBusBuilder AddBusInterceptor(BusInterceptor interceptor)
    {
        _busInterceptorRegistrations.Add(new BusInterceptorRegistration
        {
            InterceptorInstance = interceptor
        });

        return this;
    }

    /// <inheritdoc />
    public IMessageBusBuilder AddBusInterceptor(Func<IServiceProvider, BusInterceptor> factory)
    {
        _busInterceptorRegistrations.Add(new BusInterceptorRegistration
        {
            InterceptorFactory = factory
        });

        return this;
    }

    private IReadOnlyList<BusInterceptor> BuildInterceptorList(IServiceProvider services)
    {
        // Resolve each registration into a concrete interceptor instance, then filter by
        // IsEnabled, then sort ascending by Position with registration order as the tie-break.
        var resolved = new List<(BusInterceptor Interceptor, int RegistrationIndex)>(
            _busInterceptorRegistrations.Count);

        for (var i = 0; i < _busInterceptorRegistrations.Count; i++)
        {
            var registration = _busInterceptorRegistrations[i];
            BusInterceptor interceptor;

            if (registration.InterceptorInstance is not null)
            {
                interceptor = registration.InterceptorInstance;
            }
            else if (registration.InterceptorFactory is not null)
            {
                interceptor = registration.InterceptorFactory(services);
            }
            else
            {
                interceptor = (BusInterceptor)ActivatorUtilities.CreateInstance(
                    services,
                    registration.InterceptorType!);
            }

            if (interceptor.IsEnabled(services))
            {
                resolved.Add((interceptor, i));
            }
        }

        // Stable sort: ascending Position, then ascending registration index for equal positions.
        resolved.Sort(static (a, b) =>
        {
            var cmp = a.Interceptor.Position.CompareTo(b.Interceptor.Position);
            return cmp != 0 ? cmp : a.RegistrationIndex.CompareTo(b.RegistrationIndex);
        });

        var result = new BusInterceptor[resolved.Count];
        for (var i = 0; i < resolved.Count; i++)
        {
            result[i] = resolved[i].Interceptor;
        }

        return result;
    }
}
