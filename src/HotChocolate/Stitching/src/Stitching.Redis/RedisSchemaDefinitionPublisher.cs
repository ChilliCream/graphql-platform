using System.Text.Json;
using HotChocolate.Stitching.SchemaDefinitions;
using StackExchange.Redis;

namespace HotChocolate.Stitching.Redis;

public class RedisSchemaDefinitionPublisher : ISchemaDefinitionPublisher
{
    private readonly IConnectionMultiplexer _connection;
    private readonly string _configurationName;

    public RedisSchemaDefinitionPublisher(
        string configurationName,
        IConnectionMultiplexer connection)
    {
        _connection = connection;
        _configurationName = configurationName;
    }

    public async ValueTask PublishAsync(
        RemoteSchemaDefinition schemaDefinition,
        CancellationToken cancellationToken = default)
    {
        var key = $"{_configurationName}.{schemaDefinition.Name}";
        var json = SerializeSchemaDefinition(schemaDefinition);

        var database = _connection.GetDatabase();
        await database.StringSetAsync(key, json).ConfigureAwait(false);
        await database.SetAddAsync(_configurationName, schemaDefinition.Name)
            .ConfigureAwait(false);

        var subscriber = _connection.GetSubscriber();
        await subscriber.PublishAsync(_configurationName, schemaDefinition.Name)
            .ConfigureAwait(false);
    }

    private string SerializeSchemaDefinition(RemoteSchemaDefinition schemaDefinition)
    {
        var dto = new SchemaDefinitionDto
        {
            Name = schemaDefinition.Name,
            Document = schemaDefinition.Document.ToString(false),
        };

        dto.ExtensionDocuments.AddRange(
            schemaDefinition.ExtensionDocuments.Select(t => t.ToString()).ToList());

        return JsonSerializer.Serialize(dto);
    }
}
