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
    public partial class MarkClientPublishedResultParser
        : JsonResultParserBase<IMarkClientPublished>
    {
        private readonly IValueSerializer _stringSerializer;

        public MarkClientPublishedResultParser(IValueSerializerCollection serializerResolver)
        {
            if (serializerResolver is null)
            {
                throw new ArgumentNullException(nameof(serializerResolver));
            }
            _stringSerializer = serializerResolver.Get("String");
        }

        protected override IMarkClientPublished ParserData(JsonElement data)
        {
            return new MarkClientPublished
            (
                ParseMarkClientPublishedMarkSchemaPublished(data, "markSchemaPublished")
            );

        }

        private global::StrawberryShake.Tools.SchemaRegistry.IMarkSchemaPublishedPayload1 ParseMarkClientPublishedMarkSchemaPublished(
            JsonElement parent,
            string field)
        {
            JsonElement obj = parent.GetProperty(field);

            return new MarkSchemaPublishedPayload1
            (
                ParseMarkClientPublishedMarkSchemaPublishedEnvironment(obj, "environment")
            );
        }

        private global::StrawberryShake.Tools.SchemaRegistry.IEnvironmentName ParseMarkClientPublishedMarkSchemaPublishedEnvironment(
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
