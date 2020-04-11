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
    public partial class PublishClientResultParser
        : JsonResultParserBase<IPublishClient>
    {
        private readonly IValueSerializer _stringSerializer;

        public PublishClientResultParser(IValueSerializerCollection serializerResolver)
        {
            if (serializerResolver is null)
            {
                throw new ArgumentNullException(nameof(serializerResolver));
            }
            _stringSerializer = serializerResolver.Get("String");
        }

        protected override IPublishClient ParserData(JsonElement data)
        {
            return new PublishClient1
            (
                ParsePublishClientPublishClient(data, "publishClient")
            );

        }

        private global::StrawberryShake.IPublishClientPayload ParsePublishClientPublishClient(
            JsonElement parent,
            string field)
        {
            JsonElement obj = parent.GetProperty(field);

            return new PublishClientPayload
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
