using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using HotChocolate.Stitching.SchemaDefinitions;

namespace HotChocolate.Stitching.DAPR
{
    public class DAPRSchemaDefinitionPublisher : ISchemaDefinitionPublisher
    {
        private readonly NameString _configurationName;
        private readonly DaprClient _daprClient;
        private readonly string _gatewaySchemaListKey;

        public DAPRSchemaDefinitionPublisher(NameString configurationName, DaprClient daprClient)
        {
            _configurationName = configurationName;
            _daprClient = daprClient;
            _gatewaySchemaListKey = $"{_configurationName}.SchemaList";
        }

        public async ValueTask PublishAsync(
            RemoteSchemaDefinition schemaDefinition,
            CancellationToken cancellationToken = default)
        {
            string key = $"{_configurationName}.{schemaDefinition.Name}";

            await _daprClient.SaveStateAsync(DaprConfiguration.StateStoreComponent, key, schemaDefinition);

            while (!(await UpdateSchemaList(key))) { };

            await _daprClient.PublishEventAsync(DaprConfiguration.PubSubComponent, DaprConfiguration.PubSubTopic, schemaDefinition);
        }

        private async Task<bool> UpdateSchemaList(string key)
        {
            (List<string> value, string etag) items = await _daprClient.GetStateAndETagAsync<List<string>>(DaprConfiguration.StateStoreComponent, _gatewaySchemaListKey);

            if (!items.value.Contains(key))
            {
                items.value.Add(key);

                return await _daprClient.TrySaveStateAsync(DaprConfiguration.StateStoreComponent, _gatewaySchemaListKey, items.value, items.etag);
            }

            return true;
        }

    }
}
