using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;

namespace Mocha.Scheduling;

internal sealed class ScheduledMessageStoreResolver
{
    private readonly ScheduledMessageStoreRegistration[] _registrations;
    private readonly IServiceProvider _services;

    public static ScheduledMessageStoreResolver Create(IServiceProvider services)
    {
        var registrations = services.GetServices<ScheduledMessageStoreRegistration>().ToArray();
        ValidateRegistrations(registrations);

        return new ScheduledMessageStoreResolver(registrations, services);
    }

    private ScheduledMessageStoreResolver(
        ScheduledMessageStoreRegistration[] registrations,
        IServiceProvider services)
    {
        _registrations = registrations;
        _services = services;
    }

    public bool TryGetForDispatch(IDispatchContext context, [NotNullWhen(true)] out IScheduledMessageStore? store)
    {
        var transportType = context.Transport.GetType();
        ScheduledMessageStoreRegistration? fallback = null;
        ScheduledMessageStoreRegistration? match = null;

        foreach (var registration in _registrations)
        {
            if (registration.IsFallback)
            {
                fallback = registration;
                continue;
            }

            if (registration.Transport is not null)
            {
                if (!ReferenceEquals(registration.Transport, context.Transport))
                {
                    continue;
                }

                match = registration;
                break;
            }

            if (registration.TransportType!.IsAssignableFrom(transportType))
            {
                if (match is null
                    || match.TransportType!.IsAssignableFrom(registration.TransportType))
                {
                    match = registration;
                    continue;
                }

                if (!registration.TransportType.IsAssignableFrom(match.TransportType))
                {
                    throw ThrowHelper.ScheduledStoreAmbiguousMatch(transportType);
                }
            }
        }

        if (match is not null)
        {
            store = Resolve(match);
            return true;
        }

        if (fallback is not null)
        {
            store = Resolve(fallback);
            return true;
        }

        store = null;
        return false;
    }

    public async ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
    {
        foreach (var registration in _registrations)
        {
            if (token.StartsWith(registration.TokenPrefix, StringComparison.Ordinal))
            {
                var store = Resolve(registration);
                if (await store.CancelAsync(token, cancellationToken))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private IScheduledMessageStore Resolve(ScheduledMessageStoreRegistration registration)
        => registration.Resolve(_services);

    private static void ValidateRegistrations(IReadOnlyList<ScheduledMessageStoreRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            if (string.IsNullOrWhiteSpace(registration.TokenPrefix))
            {
                throw ThrowHelper.ScheduledStoreTokenPrefixEmpty();
            }

            var storeType = registration.StoreType;
            if (storeType is not null
                && !typeof(IScheduledMessageStore).IsAssignableFrom(storeType))
            {
                throw ThrowHelper.ScheduledStoreTypeInvalid(storeType);
            }

            if (registration.IsFallback)
            {
                if (registration.Transport is not null || registration.TransportType is not null)
                {
                    throw ThrowHelper.ScheduledStoreFallbackMustNotSpecifyTransport();
                }
            }
            else if (registration.Transport is null && registration.TransportType is null)
            {
                throw ThrowHelper.ScheduledStoreTransportRequired();
            }
        }

        for (var i = 0; i < registrations.Count; i++)
        {
            for (var j = i + 1; j < registrations.Count; j++)
            {
                var left = registrations[i].TokenPrefix;
                var right = registrations[j].TokenPrefix;
                if (!string.Equals(left, right, StringComparison.Ordinal)
                    && (left.StartsWith(right, StringComparison.Ordinal)
                        || right.StartsWith(left, StringComparison.Ordinal)))
                {
                    throw ThrowHelper.ScheduledStoreTokenPrefixesOverlap(left, right);
                }
            }
        }

        var duplicateTransport = registrations
            .Where(r => !r.IsFallback && r.Transport is null)
            .GroupBy(r => r.TransportType)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateTransport is not null)
        {
            throw ThrowHelper.ScheduledStoreDuplicateTransport(duplicateTransport.Key!);
        }

        var fallbackCount = registrations.Count(r => r.IsFallback);
        if (fallbackCount > 1)
        {
            throw ThrowHelper.ScheduledStoreMultipleFallbacks();
        }
    }
}
