using Aspire.Hosting;
using Aspire.Hosting.Lifecycle;
using HotChocolate.Fusion.Aspire.Nitro;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Registers the GraphQL schema composition with the distributed application. Every entry point
/// that needs the composition goes through here, so a distributed application that adds both the
/// orchestrator and Nitro still composes each gateway once.
/// </summary>
internal static class SchemaCompositionRegistration
{
    /// <summary>
    /// Registers the GraphQL schema composition unless it is already registered.
    /// </summary>
    /// <param name="builder">
    /// The distributed application builder.
    /// </param>
    public static void Ensure(IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // A second AddSingleton would register a second composition, and the two would compose
        // the same archive at the same time behind independent gates.
        builder.Services.TryAddEventingSubscriber<SchemaComposition>();
        builder.Services.TryAddSingleton<NitroSchemaValidationCoordinator>();
        builder.Services.TryAddSingleton<INitroSchemaValidationNotifier, NitroSchemaValidationNotifier>();
        builder.Services.TryAddSingleton<INitroCompositionNotifier, NitroSchemaValidationNotifier>();
        builder.Services.TryAddSingleton<INitroSeedUpdateNotifier, NitroSeedUpdateNotifier>();
        builder.Services.TryAddSingleton<NitroSeedUpdateService>();
        builder.Services.TryAddSingleton<NitroSeedCoordinatorRegistry>();
        builder.Services.TryAddSingleton<GatewayCompositionCommandCoordinator>();
        builder.Services.TryAddSingleton(TimeProvider.System);
    }
}
