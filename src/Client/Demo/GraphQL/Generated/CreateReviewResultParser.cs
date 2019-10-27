using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake;
using StrawberryShake.Http;

namespace  StrawberryShake.Client.GraphQL
{
    public class CreateReviewResultParser
        : JsonResultParserBase<ICreateReview>
    {
        private readonly IValueSerializer _stringSerializer;

        public CreateReviewResultParser(IEnumerable<IValueSerializer> serializers)
        {
            IReadOnlyDictionary<string, IValueSerializer> map = serializers.ToDictionary();

            if (!map.TryGetValue("String", out IValueSerializer? serializer))
            {
                throw new ArgumentException(
                    "There is no serializer specified for `String`.",
                    nameof(serializers));
            }
            _stringSerializer = serializer;
        }

        protected override ICreateReview ParserData(JsonElement data)
        {
            return new CreateReview1
            (
                ParseCreateReviewCreateReview(data, "createReview")
            );

        }

        private IReview ParseCreateReviewCreateReview(
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
