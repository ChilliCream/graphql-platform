using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Options;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

internal sealed partial class RequestExecutorResolver
{
    private static async ValueTask<RequestExecutorOptions> OnConfigureRequestExecutorOptionsAsync(
        ConfigurationContext context,
        RequestExecutorSetup setup,
        CancellationToken cancellationToken)
    {
        var executorOptions =
            setup.RequestExecutorOptions ??
            new RequestExecutorOptions();

        foreach (var action in setup.OnConfigureRequestExecutorOptionsHooks)
        {
            action.Configure?.Invoke(context, executorOptions);

            if (action.ConfigureAsync is not null)
            {
                await action.ConfigureAsync(context, executorOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return executorOptions;
    }

    private static void OnConfigureSchemaServices(
        ConfigurationContext context,
        IServiceCollection schemaServices,
        RequestExecutorSetup setup)
    {
        foreach (var action in setup.OnConfigureSchemaServicesHooks)
        {
            action.Invoke(context, schemaServices);
        }
    }

    private static async ValueTask OnConfigureSchemaBuilderAsync(
        ConfigurationContext context,
        IServiceProvider schemaServices,
        RequestExecutorSetup setup,
        CancellationToken cancellationToken)
    {
        foreach (var action in setup.OnConfigureSchemaBuilderHooks)
        {
            action.Configure?.Invoke(context, schemaServices);

            if (action.ConfigureAsync is not null)
            {
                await action.ConfigureAsync(context, schemaServices, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private static async ValueTask OnRequestExecutorCreatedAsync(
        ConfigurationContext context,
        IRequestExecutor requestExecutor,
        RequestExecutorSetup setup,
        CancellationToken cancellationToken)
    {
        foreach (var action in setup.OnRequestExecutorCreatedHooks)
        {
            action.Created?.Invoke(context, requestExecutor);

            if (action.CreatedAsync is not null)
            {
                await action.CreatedAsync(context, requestExecutor, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private static async ValueTask OnRequestExecutorEvictedAsync(
        RegisteredExecutor registeredExecutor)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var token = cts.Token;

        foreach (var action in registeredExecutor.Setup.OnRequestExecutorEvictedHooks)
        {
            action.Evicted?.Invoke(registeredExecutor.Executor);

            if (action.EvictedAsync is not null)
            {
                await action.EvictedAsync(registeredExecutor.Executor, token)
                    .ConfigureAwait(false);
            }
        }
    }
}
