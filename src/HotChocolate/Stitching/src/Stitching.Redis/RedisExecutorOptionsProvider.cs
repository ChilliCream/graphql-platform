using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HotChocolate.Stitching.Redis
{
    internal class RedisExecutorOptionsProvider : IRequestExecutorOptionsProvider
    {
        private readonly NameString _schemaName;
        private readonly NameString _configurationName;
        private readonly IDatabase _database;
        private readonly List<OnChangeListener> _listeners = new List<OnChangeListener>();

        public RedisExecutorOptionsProvider(
            NameString schemaName,
            NameString configurationName,
            IDatabase database,
            ISubscriber subscriber)
        {
            _schemaName = schemaName;
            _configurationName = configurationName;
            _database = database;
            subscriber.Subscribe(configurationName.Value).OnMessage(OnChangeMessageAsync);
        }

        public async ValueTask<IEnumerable<INamedRequestExecutorFactoryOptions>> GetOptionsAsync(
            CancellationToken cancellationToken)
        {
            IEnumerable<RemoteSchemaDefinition> schemaDefinitions =
                await GetSchemaDefinitionsAsync(cancellationToken)
                    .ConfigureAwait(false);

            var factoryOptions = new List<INamedRequestExecutorFactoryOptions>();

            foreach (RemoteSchemaDefinition schemaDefinition in schemaDefinitions)
            {
                await CreateFactoryOptionsAsync(
                        schemaDefinition,
                        factoryOptions,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return factoryOptions;
        }

        public IDisposable OnChange(Action<INamedRequestExecutorFactoryOptions> listener) =>
            new OnChangeListener(_listeners, listener);

        private async Task OnChangeMessageAsync(ChannelMessage message)
        {
            string schemaName = message.Message;

            RemoteSchemaDefinition schemaDefinition =
                await GetRemoteSchemaDefinitionAsync(schemaName)
                    .ConfigureAwait(false);

            var factoryOptions = new List<INamedRequestExecutorFactoryOptions>();
            await CreateFactoryOptionsAsync(schemaDefinition, factoryOptions, default)
                .ConfigureAwait(false);

            lock (_listeners)
            {
                foreach (OnChangeListener listener in _listeners)
                {
                    foreach (INamedRequestExecutorFactoryOptions options in factoryOptions)
                    {
                        listener.OnChange(options);
                    }
                }
            }
        }

        private async ValueTask<IEnumerable<RemoteSchemaDefinition>> GetSchemaDefinitionsAsync(
            CancellationToken cancellationToken)
        {
            var length = await _database.ListLengthAsync(_configurationName.Value)
                .ConfigureAwait(false);
            RedisValue[] items = await _database.ListRangeAsync(_configurationName.Value, 0, length)
                .ConfigureAwait(false);

            var schemaDefinitions = new List<RemoteSchemaDefinition>();

            foreach (var schemaName in items.Select(t => (string)t))
            {
                cancellationToken.ThrowIfCancellationRequested();

                RemoteSchemaDefinition schemaDefinition =
                    await GetRemoteSchemaDefinitionAsync(schemaName)
                        .ConfigureAwait(false);

                schemaDefinitions.Add(schemaDefinition);
            }

            return schemaDefinitions;
        }

        private async Task CreateFactoryOptionsAsync(
            RemoteSchemaDefinition schemaDefinition,
            IList<INamedRequestExecutorFactoryOptions> factoryOptions,
            CancellationToken cancellationToken)
        {
            await using ServiceProvider services =
                new ServiceCollection()
                    .AddGraphQL(_schemaName)
                    .AddRemoteSchema(
                        schemaDefinition.Name,
                        (sp, ct) => new ValueTask<RemoteSchemaDefinition>(schemaDefinition))
                    .Services
                    .BuildServiceProvider();

            IRequestExecutorOptionsMonitor optionsMonitor =
                services.GetRequiredService<IRequestExecutorOptionsMonitor>();

            RequestExecutorFactoryOptions options =
                await optionsMonitor.GetAsync(schemaDefinition.Name, cancellationToken)
                    .ConfigureAwait(false);

            factoryOptions.Add(new NamedRequestExecutorFactoryOptions(schemaDefinition.Name, options));

            options =
                await optionsMonitor.GetAsync(_schemaName, cancellationToken)
                    .ConfigureAwait(false);

            factoryOptions.Add(new NamedRequestExecutorFactoryOptions(_schemaName, options));
        }

        private async Task<RemoteSchemaDefinition> GetRemoteSchemaDefinitionAsync(string schemaName)
        {
            string key = $"{_configurationName}.{schemaName}";
            var json = (byte[])await _database.StringGetAsync(key).ConfigureAwait(false);
            SchemaDefinitionDto? dto = JsonSerializer.Deserialize<SchemaDefinitionDto>(json);

            return new RemoteSchemaDefinition(
                dto.Name,
                Utf8GraphQLParser.Parse(dto.Document),
                dto.ExtensionDocuments.Select(Utf8GraphQLParser.Parse));
        }

        private sealed class OnChangeListener : IDisposable
        {
            private readonly List<OnChangeListener> _listeners;
            private readonly Action<INamedRequestExecutorFactoryOptions> _onChange;

            public OnChangeListener(
                List<OnChangeListener> listeners,
                Action<INamedRequestExecutorFactoryOptions> onChange)
            {
                _listeners = listeners;
                _onChange = onChange;

                lock (_listeners)
                {
                    _listeners.Add(this);
                }
            }

            public void OnChange(INamedRequestExecutorFactoryOptions options) =>
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
