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

        public DAPRSchemaDefinitionPublisher(NameString configurationName)
        {
            _configurationName = configurationName;

            _daprClient = (new DaprClientBuilder()).Build();
        }

        public async ValueTask PublishAsync(
            RemoteSchemaDefinition schemaDefinition,
            CancellationToken cancellationToken = default)
        {
            string key = $"{_configurationName}.{schemaDefinition.Name}";
            //string json = SerializeSchemaDefinition(schemaDefinition);

            await _daprClient.SaveStateAsync(DaprConfiguration.StateStoreComponent, key, schemaDefinition);

            await _daprClient.PublishEventAsync(DaprConfiguration.PubSubComponent, DaprConfiguration.PubSubTopic, schemaDefinition);
        }

        //private string SerializeSchemaDefinition(RemoteSchemaDefinition schemaDefinition)
        //{
        //    var dto = new SchemaDefinitionDto
        //    {
        //        Name = schemaDefinition.Name.Value,
        //        Document = schemaDefinition.Document.ToString(false),
        //    };

        //    dto.ExtensionDocuments.AddRange(
        //        schemaDefinition.ExtensionDocuments.Select(t => t.ToString()).ToList());

        //    return JsonSerializer.Serialize(dto);
        //}
    }
}
