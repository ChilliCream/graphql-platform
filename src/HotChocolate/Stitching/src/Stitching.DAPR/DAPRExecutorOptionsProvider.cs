using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.DAPR
{
    internal class DAPRExecutorOptionsProvider : IRequestExecutorOptionsProvider
    {
        private readonly NameString _schemaName;
        private readonly NameString _configurationName;
        private readonly DaprClient _daprClient;
        private readonly List<OnChangeListener> _listeners = new List<OnChangeListener>();
        private readonly string _gatewaySchemaListKey;

        public DAPRExecutorOptionsProvider(
            NameString schemaName,
            NameString configurationName,
            DaprClient daprClient)
        {
            _schemaName = schemaName;
            _configurationName = configurationName;

            _daprClient = daprClient;

            _gatewaySchemaListKey = $"{_configurationName}.SchemaList";
        }

        public async ValueTask<IEnumerable<IConfigureRequestExecutorSetup>> GetOptionsAsync(
            CancellationToken cancellationToken)
        {
            IEnumerable<RemoteSchemaDefinition> schemaDefinitions =
                await GetSchemaDefinitionsAsync(cancellationToken)
                    .ConfigureAwait(false);

            var factoryOptions = new List<IConfigureRequestExecutorSetup>();

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

        public IDisposable OnChange(Action<IConfigureRequestExecutorSetup> listener) =>
            new OnChangeListener(_listeners, listener);

        private async Task OnChangeMessageAsync(RemoteSchemaDefinition message)
        {
            RemoteSchemaDefinition schemaDefinition = message;

            var factoryOptions = new List<IConfigureRequestExecutorSetup>();
            await CreateFactoryOptionsAsync(schemaDefinition, factoryOptions, default)
                .ConfigureAwait(false);

            lock (_listeners)
            {
                foreach (OnChangeListener listener in _listeners)
                {
                    foreach (IConfigureRequestExecutorSetup options in factoryOptions)
                    {
                        listener.OnChange(options);
                    }
                }
            }
        }

        private async ValueTask<IEnumerable<RemoteSchemaDefinition>> GetSchemaDefinitionsAsync(
            CancellationToken cancellationToken)
        {
            List<string> items = await _daprClient.GetStateAsync<List<string>>(DaprConfiguration.StateStoreComponent, _gatewaySchemaListKey) ?? new List<string>();

            var schemaDefinitions = new List<RemoteSchemaDefinition>();

            foreach (var schemaName in items)
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
            IList<IConfigureRequestExecutorSetup> factoryOptions,
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

            RequestExecutorSetup options =
                await optionsMonitor.GetAsync(schemaDefinition.Name, cancellationToken)
                    .ConfigureAwait(false);

            factoryOptions.Add(new ConfigureRequestExecutorSetup(schemaDefinition.Name, options));

            options =
                await optionsMonitor.GetAsync(_schemaName, cancellationToken)
                    .ConfigureAwait(false);

            factoryOptions.Add(new ConfigureRequestExecutorSetup(_schemaName, options));
        }

        private async Task<RemoteSchemaDefinition> GetRemoteSchemaDefinitionAsync(string schemaKey)
        {
            SchemaDefinitionDto dto = await _daprClient.GetStateAsync<SchemaDefinitionDto>(DaprConfiguration.StateStoreComponent, schemaKey, default);

            return new RemoteSchemaDefinition(
                dto.Name,
                Utf8GraphQLParser.Parse(dto.Document??string.Empty),
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
