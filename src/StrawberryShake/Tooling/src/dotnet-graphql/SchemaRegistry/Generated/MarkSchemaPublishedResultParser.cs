using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake;
using StrawberryShake.Configuration;
using StrawberryShake.Http;
using StrawberryShake.Http.Subscriptions;
using StrawberryShake.Transport;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class MarkSchemaPublishedResultParser
        : JsonResultParserBase<IMarkSchemaPublished>
    {
        private readonly IValueSerializer _stringSerializer;

        public MarkSchemaPublishedResultParser(IValueSerializerCollection serializerResolver)
        {
            if (serializerResolver is null)
            {
                throw new ArgumentNullException(nameof(serializerResolver));
            }
            _stringSerializer = serializerResolver.Get("String");
        }

        protected override IMarkSchemaPublished ParserData(JsonElement data)
        {
            return new MarkSchemaPublished1
            (
                ParseMarkSchemaPublishedMarkSchemaPublished(data, "markSchemaPublished")
            );

        }

        private global::StrawberryShake.IMarkSchemaPublishedPayload ParseMarkSchemaPublishedMarkSchemaPublished(
            JsonElement parent,
            string field)
        {
            JsonElement obj = parent.GetProperty(field);

            return new MarkSchemaPublishedPayload
            (
                ParseMarkSchemaPublishedMarkSchemaPublishedEnvironment(obj, "environment")
            );
        }

        private global::StrawberryShake.IEnvironmentName ParseMarkSchemaPublishedMarkSchemaPublishedEnvironment(
            JsonElement parent,
            string field)
        {
            JsonElement obj = parent.GetProperty(field);

            return new EnvironmentName
            (
                DeserializeString(obj, "name")
            );
        }

        private string DeserializeString(JsonElement obj, string fieldName)
        {
            JsonElement value = obj.GetProperty(fieldName);
            return (string)_stringSerializer.Deserialize(value.GetString())!;
        }
    }
}
