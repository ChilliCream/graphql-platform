using Aspire.Hosting.ApplicationModel;
using HotChocolate.Fusion.Aspire.Nitro;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// A <see cref="SchemaComposition"/> with the application host services it depends on, so a test
/// can drive the start gate and the recomposition without an application host.
/// </summary>
internal sealed record CompositionHarness(
    SchemaComposition Composition,
    NitroSchemaValidationCoordinator ValidationCoordinator,
    ResourceNotificationService Notifications,
    RecordingLogger<SchemaComposition> Logger,
    TestHostApplicationLifetime Lifetime)
{
    public static CompositionHarness Create(
        NitroSeedCoordinator? coordinator,
        Uri? portalUrl = null,
        INitroSchemaValidationNotifier? notifier = null)
    {
        var logger = new RecordingLogger<SchemaComposition>();
        var lifetime = new TestHostApplicationLifetime();
        var resourceLoggerService = new ResourceLoggerService();
        var notifications = new ResourceNotificationService(
            new RecordingLogger<ResourceNotificationService>(),
            lifetime,
            EmptyServiceProvider.Instance,
            resourceLoggerService);
        var options = new NitroCompositionOptions
        {
            Coordinator = coordinator,
            PortalUrl = portalUrl
        };
        var validationCoordinator = new NitroSchemaValidationCoordinator(
            options,
            resourceLoggerService,
            notifier,
            lifetime,
            NullLoggerFactory.Instance);
        var composition = new SchemaComposition(
            notifications,
            resourceLoggerService,
            lifetime,
            options,
            validationCoordinator,
            new GatewayCompositionCommandCoordinator(),
            logger);

        return new CompositionHarness(
            composition,
            validationCoordinator,
            notifications,
            logger,
            lifetime);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();

        public object? GetService(Type serviceType) => null;
    }
}

internal sealed class TestHostApplicationLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _stopping = new();

    public int StopApplicationCalls { get; private set; }

    public CancellationToken ApplicationStarted => CancellationToken.None;
    public CancellationToken ApplicationStopping => _stopping.Token;
    public CancellationToken ApplicationStopped => CancellationToken.None;

    public void StopApplication()
    {
        StopApplicationCalls++;
        _stopping.Cancel();
    }
}
