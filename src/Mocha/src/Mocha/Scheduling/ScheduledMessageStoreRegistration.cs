using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Scheduling;

internal sealed class ScheduledMessageStoreRegistration
{
    private readonly Func<IServiceProvider, IScheduledMessageStore> _resolveStore;

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
            storeType,
            isFallback)
    {
    }

    public ScheduledMessageStoreRegistration(
        MessagingTransport transport,
        string tokenPrefix,
        Func<IServiceProvider, IScheduledMessageStore> resolveStore)
        : this(
            transport,
            transport.GetType(),
            tokenPrefix,
            resolveStore,
            storeType: null,
            isFallback: false)
    {
    }

    private ScheduledMessageStoreRegistration(
        MessagingTransport? transport,
        Type? transportType,
        string tokenPrefix,
        Func<IServiceProvider, IScheduledMessageStore> resolveStore,
        Type? storeType,
        bool isFallback)
    {
        Transport = transport;
        TransportType = transportType;
        TokenPrefix = tokenPrefix;
        StoreType = storeType;
        IsFallback = isFallback;
        _resolveStore = resolveStore;
    }

    public MessagingTransport? Transport { get; }

    public Type? TransportType { get; }

    public string TokenPrefix { get; }

    public Type? StoreType { get; }

    public bool IsFallback { get; }

    public IScheduledMessageStore Resolve(IServiceProvider services)
        => _resolveStore(services);
}
