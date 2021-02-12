using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Misc;

namespace HotChocolate.Data.MongoDb
{
    internal sealed class MongoDbIncludeProjectionOperation : MongoDbProjectionDefinition
    {
        private readonly string _path;
        private readonly SortDirection _direction;

        public MongoDbIncludeProjectionOperation (
            string field)
        {
            _path = Ensure.IsNotNull(field, nameof(field));
        }

        public override BsonDocument Render(
            IBsonSerializer documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            StringFieldDefinitionHelper.Resolve(
                _path,
                documentSerializer,
                out string? resolvedFieldName,
                out IBsonSerializer? _);

            return new BsonDocument(resolvedFieldName ?? _path, 1);
        }
    }
}
