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
    public partial class PublishSchemaResultParser
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

        private global::StrawberryShake.IPublishSchemaPayload ParsePublishSchemaPublishSchema(
            JsonElement parent,
            string field)
        {
            JsonElement obj = parent.GetProperty(field);

            return new PublishSchemaPayload
            (
                DeserializeString(obj, "sessionId")
            );
        }

        private string DeserializeString(JsonElement obj, string fieldName)
        {
            JsonElement value = obj.GetProperty(fieldName);
            return (string)_stringSerializer.Deserialize(value.GetString())!;
        }
    }
}
