using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;

namespace Mocha.Scheduling;

internal sealed class ScheduledMessageStoreResolver : IScheduledMessageStoreResolver
{
    private readonly IServiceProvider _services;
    private readonly IReadOnlyList<ScheduledMessageStoreRegistration> _registrations;

    public ScheduledMessageStoreResolver(
        IServiceProvider services,
        IEnumerable<ScheduledMessageStoreRegistration> registrations)
    {
        _services = services;
        _registrations = registrations.ToArray();

        ScheduledMessageStoreRegistrationValidator.Validate(_registrations);
    }

    public bool TryGetForDispatch(IDispatchContext context, out IScheduledMessageStore store)
    {
        var transportType = context.Transport.GetType();

        ScheduledMessageStoreRegistration? match = null;
        ScheduledMessageStoreRegistration? fallback = null;

        foreach (var registration in _registrations)
        {
            if (registration.IsFallback)
            {
                fallback = registration;
                continue;
            }

            if (registration.TransportType is { } owner && owner.IsAssignableFrom(transportType))
            {
                match = registration;
                break;
            }
        }

        var selected = match ?? fallback;
        if (selected is null)
        {
            store = null!;
            return false;
        }

        store = (IScheduledMessageStore)_services.GetRequiredService(selected.StoreType);
        return true;
    }

    public bool TryGetForCancellation(string token, out IScheduledMessageStore store)
    {
        if (string.IsNullOrEmpty(token))
        {
            store = null!;
            return false;
        }

        foreach (var registration in _registrations)
        {
            if (token.StartsWith(registration.TokenPrefix, StringComparison.Ordinal))
            {
                store = (IScheduledMessageStore)_services.GetRequiredService(registration.StoreType);
                return true;
            }
        }

        store = null!;
        return false;
    }
}
