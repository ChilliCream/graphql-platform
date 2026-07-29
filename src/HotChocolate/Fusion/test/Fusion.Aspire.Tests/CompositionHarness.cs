using Aspire.Hosting.ApplicationModel;
using HotChocolate.Fusion.Aspire.Nitro;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// A <see cref="SchemaComposition"/> with the application host services it depends on, so a test
/// can drive the start gate and the recomposition without an application host.
/// </summary>
internal sealed record CompositionHarness(
    SchemaComposition Composition,
    ResourceNotificationService Notifications,
    RecordingLogger<SchemaComposition> Logger,
    TestHostApplicationLifetime Lifetime)
{
    public static CompositionHarness Create(NitroSeedCoordinator? coordinator)
    {
        var logger = new RecordingLogger<SchemaComposition>();
        var lifetime = new TestHostApplicationLifetime();
        var resourceLoggerService = new ResourceLoggerService();
        var notifications = new ResourceNotificationService(
            new RecordingLogger<ResourceNotificationService>(),
            lifetime,
            EmptyServiceProvider.Instance,
            resourceLoggerService);
        var composition = new SchemaComposition(
            notifications,
            resourceLoggerService,
            lifetime,
            new NitroCompositionOptions { Coordinator = coordinator },
            logger);

        return new CompositionHarness(composition, notifications, logger, lifetime);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();

        public object? GetService(Type serviceType) => null;
    }
}

internal sealed class TestHostApplicationLifetime : IHostApplicationLifetime
{
    public int StopApplicationCalls { get; private set; }

    public CancellationToken ApplicationStarted => CancellationToken.None;
    public CancellationToken ApplicationStopping => CancellationToken.None;
    public CancellationToken ApplicationStopped => CancellationToken.None;

    public void StopApplication() => StopApplicationCalls++;
}
