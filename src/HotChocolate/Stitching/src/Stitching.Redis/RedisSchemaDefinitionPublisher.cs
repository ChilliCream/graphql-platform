using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Stitching.SchemaDefinitions;
using StackExchange.Redis;

namespace HotChocolate.Stitching.Redis
{
    public class RedisSchemaDefinitionPublisher : ISchemaDefinitionPublisher
    {
        private readonly IConnectionMultiplexer _connection;
        private readonly NameString _configurationName;

        public RedisSchemaDefinitionPublisher(
            NameString configurationName,
            IConnectionMultiplexer connection)
        {
            _connection = connection;
            _configurationName = configurationName;
        }

        public async ValueTask PublishAsync(
            RemoteSchemaDefinition schemaDefinition,
            CancellationToken cancellationToken = default)
        {
            string key = $"{_configurationName}.{schemaDefinition.Name}";
            string json = SerializeSchemaDefinition(schemaDefinition);

            IDatabase database = _connection.GetDatabase();
            await database.StringSetAsync(key, json).ConfigureAwait(false);
            await database.SetAddAsync(_configurationName.Value, schemaDefinition.Name.Value)
                .ConfigureAwait(false);

            ISubscriber subscriber = _connection.GetSubscriber();
            await subscriber.PublishAsync(_configurationName.Value, schemaDefinition.Name.Value)
                .ConfigureAwait(false);
        }

        private string SerializeSchemaDefinition(RemoteSchemaDefinition schemaDefinition)
        {
            var dto = new SchemaDefinitionDto
            {
                Name = schemaDefinition.Name.Value,
                Document = schemaDefinition.Document.ToString(false),
            };

            dto.ExtensionDocuments.AddRange(
                schemaDefinition.ExtensionDocuments.Select(t => t.ToString()).ToList());

            return JsonSerializer.Serialize(dto);
        }
    }
}
