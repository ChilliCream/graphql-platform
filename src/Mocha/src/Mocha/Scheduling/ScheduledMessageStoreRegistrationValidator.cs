namespace Mocha.Scheduling;

internal static class ScheduledMessageStoreRegistrationValidator
{
    public static void Validate(IReadOnlyList<ScheduledMessageStoreRegistration> registrations)
    {
        var fallbackCount = 0;
        var seenTransports = new HashSet<Type>();
        var seenPrefixes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var registration in registrations)
        {
            if (!typeof(IScheduledMessageStore).IsAssignableFrom(registration.StoreType))
            {
                throw new InvalidOperationException(
                    $"Scheduled message store '{registration.StoreType}' does not implement "
                    + $"'{nameof(IScheduledMessageStore)}'.");
            }

            if (!seenPrefixes.Add(registration.TokenPrefix))
            {
                throw new InvalidOperationException(
                    $"Duplicate scheduled message store token prefix '{registration.TokenPrefix}'.");
            }

            if (registration.IsFallback)
            {
                fallbackCount++;
                if (fallbackCount > 1)
                {
                    throw new InvalidOperationException(
                        "More than one fallback scheduled message store is registered.");
                }

                continue;
            }

            if (registration.TransportType is null)
            {
                throw new InvalidOperationException(
                    $"Scheduled message store '{registration.StoreType}' is not marked as fallback "
                    + "but has no transport type.");
            }

            if (!seenTransports.Add(registration.TransportType))
            {
                throw new InvalidOperationException(
                    "Duplicate non-fallback scheduled message store registration for transport "
                    + $"'{registration.TransportType}'.");
            }
        }
    }
}
