using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class GatewayConfigurationTypeModule : TypeModule
{
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private DocumentNode? _configuration;

    public GatewayConfigurationTypeModule(IObservable<GatewayConfiguration> configuration)
    {
        configuration.Subscribe(
            config =>
            {
                _configuration = config.Document;
                _ready.TrySetResult();
                OnTypesChanged();
            },
            error => _ready.TrySetException(error),
            () => _ready.TrySetCanceled());
    }

    internal override async ValueTask ConfigureAsync(ConfigurationContext context, CancellationToken cancellationToken)
    {
        await _ready.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        if (_configuration is null)
        {
            throw ThrowHelper.UnableToLoadConfiguration();
        }

        var rewriter = new FusionGraphConfigurationToSchemaRewriter();
        var fusionGraphConfig = FusionGraphConfiguration.Load(_configuration);
        var schemaDoc = rewriter.Rewrite(_configuration);

        context.SchemaBuilder
            .AddDocument(schemaDoc)
            .SetFusionGraphConfig(fusionGraphConfig);
    }
}
