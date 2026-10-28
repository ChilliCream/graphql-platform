using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Scheduling;

internal sealed class ScheduledMessageStoreRegistration
{
    private static readonly Func<bool> s_alwaysAvailable = static () => true;

    private readonly Func<IServiceProvider, IScheduledMessageStore> _resolveStore;
    private readonly Func<bool> _isAvailable;

    public ScheduledMessageStoreRegistration(
        Type? transportType,
        string tokenPrefix,
        Type storeType,
        bool isFallback = false)
        : this(
            transport: null,
            transportType,
            tokenPrefix,
            services => (IScheduledMessageStore)services.GetRequiredService(storeType),
            s_alwaysAvailable,
            storeType,
            isFallback)
    {
    }

    public ScheduledMessageStoreRegistration(
        MessagingTransport transport,
        string tokenPrefix,
        Func<IServiceProvider, IScheduledMessageStore> resolveStore)
        : this(transport, tokenPrefix, resolveStore, s_alwaysAvailable)
    {
    }

    /// <summary>
    /// Creates a registration bound to <paramref name="transport"/> that only claims scheduled
    /// dispatches while <paramref name="isAvailable"/> returns <see langword="true"/>. Otherwise the
    /// resolver moves on to the fallback store.
    /// </summary>
    public ScheduledMessageStoreRegistration(
        MessagingTransport transport,
        string tokenPrefix,
        Func<IServiceProvider, IScheduledMessageStore> resolveStore,
        Func<bool> isAvailable)
        : this(
            transport,
            transport.GetType(),
            tokenPrefix,
            resolveStore,
            isAvailable,
            storeType: null,
            isFallback: false)
    {
    }

    private ScheduledMessageStoreRegistration(
        MessagingTransport? transport,
        Type? transportType,
        string tokenPrefix,
        Func<IServiceProvider, IScheduledMessageStore> resolveStore,
        Func<bool> isAvailable,
        Type? storeType,
        bool isFallback)
    {
        Transport = transport;
        TransportType = transportType;
        TokenPrefix = tokenPrefix;
        StoreType = storeType;
        IsFallback = isFallback;
        _resolveStore = resolveStore;
        _isAvailable = isAvailable;
    }

    public MessagingTransport? Transport { get; }

    public Type? TransportType { get; }

    public string TokenPrefix { get; }

    public Type? StoreType { get; }

    public bool IsFallback { get; }

    public bool IsAvailable => _isAvailable();

    public IScheduledMessageStore Resolve(IServiceProvider services)
        => _resolveStore(services);
}
