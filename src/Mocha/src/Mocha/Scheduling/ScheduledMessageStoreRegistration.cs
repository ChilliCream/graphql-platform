namespace Mocha.Scheduling;

/// <summary>
/// Metadata describing a scheduled-message store registration so the
/// <see cref="IScheduledMessageStoreResolver"/> can route dispatches and cancellations.
/// </summary>
/// <param name="TransportType">
/// The concrete <see cref="MessagingTransport"/> type this store handles, or <see langword="null"/>
/// when this is a fallback store usable for any transport that has no specific registration.
/// </param>
/// <param name="TokenPrefix">
/// The prefix that all cancellation tokens returned by this store carry, including the trailing
/// colon (for example <c>"asb:"</c> or <c>"postgres-scheduler:"</c>).
/// </param>
/// <param name="StoreType">
/// The CLR type of the store implementation. Must implement <see cref="IScheduledMessageStore"/>.
/// </param>
/// <param name="IsFallback">
/// Whether this registration is the fallback store. Exactly one fallback may be registered.
/// </param>
internal sealed record ScheduledMessageStoreRegistration(
    Type? TransportType,
    string TokenPrefix,
    Type StoreType,
    bool IsFallback = false);
