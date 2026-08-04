using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using HotChocolate.Fusion.Aspire.Nitro;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        INitroSchemaValidationNotifier? notifier = null,
        INitroCompositionNotifier? compositionNotifier = null,
        bool waitForRunningState = false)
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
        options.SeedUpdates.Enabled = false;
        var validationCoordinator = new NitroSchemaValidationCoordinator(
            options,
            resourceLoggerService,
            notifier,
            lifetime,
            NullLoggerFactory.Instance);
        var seedUpdateService = new NitroSeedUpdateService(
            options,
            resourceLoggerService,
            NoopSeedUpdateNotifier.Instance,
            lifetime,
            NullLoggerFactory.Instance,
            TimeProvider.System);
        var resolvedCompositionNotifier = compositionNotifier
            ?? notifier as INitroCompositionNotifier
            ?? NoopCompositionNotifier.Instance;
        var commandCoordinator = new GatewayCompositionCommandCoordinator();
        var composition = waitForRunningState
            ? new ResourceStateSchemaComposition(
                notifications,
                resourceLoggerService,
                lifetime,
                options,
                resolvedCompositionNotifier,
                validationCoordinator,
                seedUpdateService,
                commandCoordinator,
                logger)
            : new SchemaComposition(
                notifications,
                resourceLoggerService,
                lifetime,
                options,
                resolvedCompositionNotifier,
                validationCoordinator,
                seedUpdateService,
                commandCoordinator,
                logger);

        return new CompositionHarness(
            composition,
            validationCoordinator,
            notifications,
            logger,
            lifetime);
    }

    /// <summary>
    /// A <see cref="SchemaComposition"/> that treats a source schema resource as ready once it
    /// reports that it runs. Aspire only fires the resource ready event that the health wait waits
    /// for from its orchestrator, which a harness without an application host cannot run.
    /// </summary>
    private sealed class ResourceStateSchemaComposition : SchemaComposition
    {
        private readonly ResourceNotificationService _notifications;

        public ResourceStateSchemaComposition(
            ResourceNotificationService resourceNotificationService,
            ResourceLoggerService resourceLoggerService,
            IHostApplicationLifetime lifetime,
            NitroCompositionOptions nitroOptions,
            INitroCompositionNotifier nitroCompositionNotifier,
            NitroSchemaValidationCoordinator validationCoordinator,
            NitroSeedUpdateService seedUpdateService,
            GatewayCompositionCommandCoordinator commandCoordinator,
            ILogger<SchemaComposition> logger)
            : base(
                resourceNotificationService,
                resourceLoggerService,
                lifetime,
                nitroOptions,
                nitroCompositionNotifier,
                validationCoordinator,
                seedUpdateService,
                commandCoordinator,
                logger)
        {
            _notifications = resourceNotificationService;
        }

        protected internal override async Task WaitForResourceHealthyAsync(
            string resourceName,
            CancellationToken cancellationToken)
        {
            var state = await _notifications.WaitForResourceAsync(
                resourceName,
                [KnownResourceStates.Running, .. KnownResourceStates.TerminalStates],
                cancellationToken);

            if (state != KnownResourceStates.Running)
            {
                throw new DistributedApplicationException(
                    $"The resource '{resourceName}' reached the state '{state}'.");
            }
        }
    }

    private sealed class NoopSeedUpdateNotifier : INitroSeedUpdateNotifier
    {
        public static NoopSeedUpdateNotifier Instance { get; } = new();

        public void NotifyAdopted(string message)
        {
        }

        public void NotifyStaged(string message)
        {
        }
    }

    private sealed class NoopCompositionNotifier : INitroCompositionNotifier
    {
        public static NoopCompositionNotifier Instance { get; } = new();

        public void NotifyFailure(string gatewayName, string message)
        {
        }
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
