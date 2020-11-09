using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Misc;

namespace HotChocolate.MongoDb.Data
{
    /// <summary>
    /// This class was ported over from the official mongo db driver
    /// </summary>
    internal sealed class MongoDbDirectionalSortOperation : MongoDbSortDefinition
    {
        private readonly string _path;
        private readonly SortDirection _direction;

        public MongoDbDirectionalSortOperation(
            string field,
            SortDirection direction)
        {
            _path = Ensure.IsNotNull(field, nameof(field));
            _direction = direction;
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

            BsonValue value;
            switch (_direction)
            {
                case SortDirection.Ascending:
                    value = 1;
                    break;
                case SortDirection.Descending:
                    value = -1;
                    break;
                default:
                    throw new InvalidOperationException(
                        "Unknown value for " + typeof(SortDirection) + ".");
            }

            return new BsonDocument(resolvedFieldName ?? _path, value);
        }
    }
}
