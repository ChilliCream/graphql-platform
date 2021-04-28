using System;
using System.Collections;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace HotChocolate.Data.MongoDb
{
    internal class MongoDbFilterOperation : MongoDbFilterDefinition
    {
        private readonly string _path;
        private readonly object? _value;

        public MongoDbFilterOperation(string path, object? value)
        {
            _path = path;
            _value = value;
        }

        public override BsonDocument Render(
            IBsonSerializer documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            StringFieldDefinitionHelper.Resolve(
                _path,
                documentSerializer,
                out string? resolvedFieldName,
                out IBsonSerializer? resolvedFieldSerializer);

            resolvedFieldSerializer ??= documentSerializer;

            if (_value is BsonDocument bsonDocument)
            {
                return new BsonDocument(resolvedFieldName, bsonDocument);
            }

            if (_value is BsonValue bsonValue)
            {
                return new BsonDocument(resolvedFieldName, bsonValue);
            }

            if (_value is MongoDbFilterDefinition mongoDbOperation)
            {
                if (_path is "")
                {
                    return mongoDbOperation.Render(resolvedFieldSerializer, serializerRegistry);
                }

                return new BsonDocument(
                    resolvedFieldName,
                    mongoDbOperation.Render(resolvedFieldSerializer, serializerRegistry));
            }

            var document = new BsonDocument();
            using var bsonWriter = new BsonDocumentWriter(document);
            var context = BsonSerializationContext.CreateRoot(bsonWriter);
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteName(resolvedFieldName);
            if (_value is IList values)
            {
                bsonWriter.WriteStartArray();
                foreach (var value in values)
                {
                    resolvedFieldSerializer.Serialize(context, value);
                }

                bsonWriter.WriteEndArray();
            }
            else
            {
                if (_value is DateTimeOffset dateTimeOffset &&
                    resolvedFieldSerializer is DateTimeSerializer or NullableSerializer<DateTime>)
                {
                    if (dateTimeOffset.Offset == TimeSpan.Zero)
                    {
                        resolvedFieldSerializer.Serialize(context, dateTimeOffset.UtcDateTime);
                    }
                    else
                    {
                        resolvedFieldSerializer.Serialize(context, dateTimeOffset.DateTime);
                    }
                }
                else
                {
                    resolvedFieldSerializer.Serialize(context, _value);
                }
            }

            bsonWriter.WriteEndDocument();

            return document;
        }
    }
}
