using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake;
using StrawberryShake.Configuration;
using StrawberryShake.Http;
using StrawberryShake.Http.Subscriptions;
using StrawberryShake.Transport;

namespace StrawberryShake.Tools.SchemaRegistry
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public class PublishSchemaResultParser
        : JsonResultParserBase<IPublishSchema>
    {
        private readonly IValueSerializer _stringSerializer;

        public PublishSchemaResultParser(IValueSerializerCollection serializerResolver)
        {
            if (serializerResolver is null)
            {
                throw new ArgumentNullException(nameof(serializerResolver));
            }
            _stringSerializer = serializerResolver.Get("String");
        }

        protected override IPublishSchema ParserData(JsonElement data)
        {
            return new PublishSchema1
            (
                ParsePublishSchemaPublishSchema(data, "publishSchema")
            );

        }

        private IPublishSchemaPayload ParsePublishSchemaPublishSchema(
            JsonElement parent,
            string field)
        {
            JsonElement obj = parent.GetProperty(field);

            return new PublishSchemaPayload
            (
                ParsePublishSchemaPublishSchemaReport(obj, "report"),
                DeserializeNullableString(obj, "clientMutationId")
            );
        }

        private ISchemaPublishReport ParsePublishSchemaPublishSchemaReport(
            JsonElement parent,
            string field)
        {
            JsonElement obj = parent.GetProperty(field);

            return new SchemaPublishReport
            (
                ParsePublishSchemaPublishSchemaReportEnvironment(obj, "environment"),
                ParsePublishSchemaPublishSchemaReportSchemaVersion(obj, "schemaVersion")
            );
        }

        private IEnvironment ParsePublishSchemaPublishSchemaReportEnvironment(
            JsonElement parent,
            string field)
        {
            JsonElement obj = parent.GetProperty(field);

            return new Environment
            (
                DeserializeString(obj, "name")
            );
        }

        private ISchemaVersion ParsePublishSchemaPublishSchemaReportSchemaVersion(
            JsonElement parent,
            string field)
        {
            JsonElement obj = parent.GetProperty(field);

            return new SchemaVersion
            (
                DeserializeString(obj, "hash")
            );
        }

        private string? DeserializeNullableString(JsonElement obj, string fieldName)
        {
            if (!obj.TryGetProperty(fieldName, out JsonElement value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return (string?)_stringSerializer.Deserialize(value.GetString())!;
        }
        private string DeserializeString(JsonElement obj, string fieldName)
        {
            JsonElement value = obj.GetProperty(fieldName);
            return (string)_stringSerializer.Deserialize(value.GetString())!;
        }
    }
}
