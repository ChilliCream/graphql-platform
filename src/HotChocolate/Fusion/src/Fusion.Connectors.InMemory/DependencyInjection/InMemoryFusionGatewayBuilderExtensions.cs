using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Connectors.InMemory;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Options;
using HotChocolate.Transport.Formatters;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the in-memory connector on <see cref="IFusionGatewayBuilder"/>.
/// </summary>
public static class InMemoryFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Registers a callback to modify the options used to compose in-memory source schemas.
    /// </summary>
    /// <param name="builder">The fusion gateway builder.</param>
    /// <param name="configure">A delegate that modifies the schema composer options.</param>
    /// <returns>The fusion gateway builder for chaining.</returns>
    public static IFusionGatewayBuilder ModifyInMemoryCompositionOptions(
        this IFusionGatewayBuilder builder,
        Action<SchemaComposerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.Configure(configure);
        return builder;
    }

    /// <summary>
    /// Adds an in-memory schema connector that executes operations directly in-process
    /// against the schema registered by the given <paramref name="schemaBuilder"/>.
    /// </summary>
    /// <param name="builder">The fusion gateway builder.</param>
    /// <param name="schemaBuilder">
    /// The request executor builder whose <see cref="IRequestExecutorBuilder.Name"/>
    /// identifies the source schema.
    /// </param>
    /// <returns>The fusion gateway builder for chaining.</returns>
    public static IFusionGatewayBuilder AddInMemorySchema(
        this IFusionGatewayBuilder builder,
        IRequestExecutorBuilder schemaBuilder)
    {
        ArgumentNullException.ThrowIfNull(schemaBuilder);

        return builder.AddInMemorySchema(schemaBuilder.Name);
    }

    /// <summary>
    /// Adds an in-memory schema connector that executes operations directly in-process
    /// against the schema identified by <paramref name="schemaName"/>.
    /// </summary>
    /// <param name="builder">The fusion gateway builder.</param>
    /// <param name="schemaName">The name of the source schema.</param>
    /// <returns>The fusion gateway builder for chaining.</returns>
    public static IFusionGatewayBuilder AddInMemorySchema(
        this IFusionGatewayBuilder builder,
        string schemaName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(schemaName);

        builder.Services.AddOptions<SchemaComposerOptions>();

        if (!builder.Services.Any(d => d.ServiceType == typeof(InMemorySourceSchemaClientFactory)))
        {
            // Remove default HTTP client factory — in-memory mode doesn't need it.
            for (var i = builder.Services.Count - 1; i >= 0; i--)
            {
                if (builder.Services[i].ServiceType == typeof(ISourceSchemaClientFactory))
                {
                    builder.Services.RemoveAt(i);
                }
            }

            builder.Services.AddSingleton(
                static sp => new InMemorySourceSchemaClientFactory(
                    sp.GetRequiredService<IRequestExecutorProvider>(),
                    sp.GetRequiredService<IRequestExecutorEvents>(),
                    JsonResultFormatter.Default));

            builder.Services.AddSingleton<ISourceSchemaClientFactory>(
                static sp => sp.GetRequiredService<InMemorySourceSchemaClientFactory>());
        }

        builder.Services.AddSingleton(new InMemorySchemaRegistration(schemaName));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.DocumentProvider = sp =>
            {
                var names = sp.GetServices<InMemorySchemaRegistration>()
                    .Select(r => r.SchemaName).ToArray();
                return new InMemoryConfigurationProvider(
                    names,
                    sp.GetRequiredService<IRequestExecutorProvider>(),
                    sp.GetRequiredService<IRequestExecutorEvents>(),
                    sp.GetRequiredService<IOptions<SchemaComposerOptions>>().Value);
            });

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new InMemorySourceSchemaClientConfiguration(schemaName)));

        return builder;
    }
}

internal sealed record InMemorySchemaRegistration(string SchemaName);
