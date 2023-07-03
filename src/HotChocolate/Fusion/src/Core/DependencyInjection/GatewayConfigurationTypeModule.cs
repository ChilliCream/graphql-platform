using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static System.Threading.Tasks.TaskCreationOptions;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class GatewayConfigurationTypeModule : TypeModule
{
    private readonly IEnumerable<IConfigurationRewriter> _configurationRewriters;
    private readonly TaskCompletionSource _ready = new(RunContinuationsAsynchronously);
    private DocumentNode? _configuration;

    public GatewayConfigurationTypeModule(
        IObservable<GatewayConfiguration> configuration,
        IEnumerable<IConfigurationRewriter> configurationRewriters)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _configurationRewriters = configurationRewriters ??
            throw new ArgumentNullException(nameof(configurationRewriters));

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

    internal override async ValueTask ConfigureAsync(
        ConfigurationContext context,
        CancellationToken cancellationToken)
    {
        await _ready.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        if (_configuration is null)
        {
            throw ThrowHelper.UnableToLoadConfiguration();
        }

        var config = _configuration;

        foreach (var rewriter in _configurationRewriters)
        {
            config = await rewriter.RewriteAsync(config, cancellationToken)
                .ConfigureAwait(false);
        }

        ApplyConfiguration(context.SchemaBuilder, config);
    }

    private void ApplyConfiguration(ISchemaBuilder schemaBuilder, DocumentNode config)
    {
        var rewriter = new FusionGraphConfigurationToSchemaRewriter();
        var fusionGraphConfig = FusionGraphConfiguration.Load(config);
        var schemaDoc = rewriter.Rewrite(config);

        schemaBuilder
            .AddDocument(schemaDoc)
            .SetFusionGraphConfig(fusionGraphConfig);
    }
}
