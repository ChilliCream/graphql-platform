using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake;
using StrawberryShake.Http;
using StrawberryShake.Http.Subscriptions;
using StrawberryShake.Transport;

namespace StrawberryShake.Client.GraphQL
{
    public class OnReviewResultParser
        : JsonResultParserBase<IOnReview>
    {
        private readonly IValueSerializer _stringSerializer;

        public OnReviewResultParser(IValueSerializerResolver serializerResolver)
        {
            if (serializerResolver is null)
            {
                throw new ArgumentNullException(nameof(serializerResolver));
            }
            _stringSerializer = serializerResolver.GetValueSerializer("String");
        }

        protected override IOnReview ParserData(JsonElement data)
        {
            return new OnReview1
            (
                ParseOnReviewOnReview(data, "onReview")
            );

        }

        private IReview ParseOnReviewOnReview(
            JsonElement parent,
            string field)
        {
            JsonElement obj = parent.GetProperty(field);

            return new Review
            (
                DeserializeNullableString(obj, "commentary")
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
    }
}
