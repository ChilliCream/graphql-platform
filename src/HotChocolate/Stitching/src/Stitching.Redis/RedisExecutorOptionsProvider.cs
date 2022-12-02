using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using StackExchange.Redis;

namespace HotChocolate.Stitching.Redis
{
    internal class RedisExecutorOptionsProvider : IRequestExecutorOptionsProvider
    {
        private readonly string _schemaName;
        private readonly string _configurationName;
        private readonly IDatabase _database;
        private readonly List<OnChangeListener> _listeners = new();

        public RedisExecutorOptionsProvider(
            string schemaName,
            string configurationName,
            IDatabase database,
            ISubscriber subscriber)
        {
            _schemaName = schemaName;
            _configurationName = configurationName;
            _database = database;
            subscriber.Subscribe(configurationName).OnMessage(OnChangeMessageAsync);
        }

        public async ValueTask<IEnumerable<IConfigureRequestExecutorSetup>> GetOptionsAsync(
            CancellationToken cancellationToken)
        {
            var schemaDefinitions =
                await GetSchemaDefinitionsAsync(cancellationToken)
                    .ConfigureAwait(false);

            var factoryOptions = new List<IConfigureRequestExecutorSetup>();

            foreach (var schemaDefinition in schemaDefinitions)
            {
                await CreateFactoryOptionsAsync(
                    schemaDefinition,
                    factoryOptions,
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            return factoryOptions;
        }

        public IDisposable OnChange(Action<IConfigureRequestExecutorSetup> listener) =>
            new OnChangeListener(_listeners, listener);

        private async Task OnChangeMessageAsync(ChannelMessage message)
        {
            string schemaName = message.Message;

            var schemaDefinition =
                await GetRemoteSchemaDefinitionAsync(schemaName)
                    .ConfigureAwait(false);

            var factoryOptions = new List<IConfigureRequestExecutorSetup>();
            await CreateFactoryOptionsAsync(schemaDefinition, factoryOptions, default)
                .ConfigureAwait(false);

            lock (_listeners)
            {
                foreach (var listener in _listeners)
                {
                    foreach (var options in factoryOptions)
                    {
                        listener.OnChange(options);
                    }
                }
            }
        }

        private async ValueTask<IEnumerable<RemoteSchemaDefinition>> GetSchemaDefinitionsAsync(
            CancellationToken cancellationToken)
        {
            var items = await _database.SetMembersAsync(_configurationName)
                .ConfigureAwait(false);

            var schemaDefinitions = new List<RemoteSchemaDefinition>();

            foreach (var schemaName in items.Select(t => (string)t))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var schemaDefinition =
                    await GetRemoteSchemaDefinitionAsync(schemaName)
                        .ConfigureAwait(false);

                schemaDefinitions.Add(schemaDefinition);
            }

            return schemaDefinitions;
        }

        private async Task CreateFactoryOptionsAsync(
            RemoteSchemaDefinition schemaDefinition,
            IList<IConfigureRequestExecutorSetup> factoryOptions,
            CancellationToken cancellationToken)
        {
            await using var services =
                new ServiceCollection()
                    .AddGraphQL(_schemaName)
                    .AddRemoteSchema(
                        schemaDefinition.Name,
                        (_, _) => new ValueTask<RemoteSchemaDefinition>(schemaDefinition))
                    .Services
                    .BuildServiceProvider();

            var optionsMonitor =
                services.GetRequiredService<IRequestExecutorOptionsMonitor>();

            var options =
                await optionsMonitor.GetAsync(schemaDefinition.Name, cancellationToken)
                    .ConfigureAwait(false);

            factoryOptions.Add(new ConfigureRequestExecutorSetup(schemaDefinition.Name, options));

            options =
                await optionsMonitor.GetAsync(_schemaName, cancellationToken)
                    .ConfigureAwait(false);

            factoryOptions.Add(new ConfigureRequestExecutorSetup(_schemaName, options));
        }

        private async Task<RemoteSchemaDefinition> GetRemoteSchemaDefinitionAsync(string schemaName)
        {
            var key = $"{_configurationName}.{schemaName}";
            var json = (byte[])await _database.StringGetAsync(key).ConfigureAwait(false);
            var dto = JsonSerializer.Deserialize<SchemaDefinitionDto>(json);

            return new RemoteSchemaDefinition(
                dto.Name,
                Utf8GraphQLParser.Parse(dto.Document),
                dto.ExtensionDocuments.Select(Utf8GraphQLParser.Parse));
        }

        private sealed class OnChangeListener : IDisposable
        {
            private readonly List<OnChangeListener> _listeners;
            private readonly Action<IConfigureRequestExecutorSetup> _onChange;

            public OnChangeListener(
                List<OnChangeListener> listeners,
                Action<IConfigureRequestExecutorSetup> onChange)
            {
                _listeners = listeners;
                _onChange = onChange;

                lock (_listeners)
                {
                    _listeners.Add(this);
                }
            }

            public void OnChange(IConfigureRequestExecutorSetup options) =>
                _onChange(options);

            public void Dispose()
            {
                lock (_listeners)
                {
                    _listeners.Remove(this);
                }
            }
        }
    }
}
