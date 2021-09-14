using System.Collections.Generic;
using System.Linq;
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

            await _daprClient.SaveStateAsync(DaprConfiguration.StateStoreComponent, key, new SchemaDefinitionDto {Document = schemaDefinition.Document.ToString(), ExtensionDocuments = schemaDefinition.ExtensionDocuments.Select(_ => _.ToString()).ToList(), Name = schemaDefinition.Name });

            while (!(await UpdateSchemaList(key))) { };

            await _daprClient.PublishEventAsync(DaprConfiguration.PubSubComponent, DaprConfiguration.PubSubTopic, schemaDefinition);
        }

        private async Task<bool> UpdateSchemaList(string key)
        {
            (List<string> value, string etag) items = await _daprClient.GetStateAndETagAsync<List<string>>(DaprConfiguration.StateStoreComponent, _gatewaySchemaListKey);

            List<string> newValues = new List<string>();

            if (items.value is not null && items.value.Any())
            {
                newValues.AddRange(items.value);
            }

            if (!newValues.Contains(key))
            {
                newValues.Add(key);

                return await _daprClient.TrySaveStateAsync(DaprConfiguration.StateStoreComponent, _gatewaySchemaListKey, newValues, items.etag ?? string.Empty);
            }

            return true;
        }

    }
}
