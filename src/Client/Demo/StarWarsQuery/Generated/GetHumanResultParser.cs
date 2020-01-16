using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake;
using StrawberryShake.Configuration;
using StrawberryShake.Http;
using StrawberryShake.Http.Subscriptions;
using StrawberryShake.Transport;

namespace StrawberryShake.Client.StarWarsQuery
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHumanResultParser
        : JsonResultParserBase<IGetHuman>
    {
        private readonly IValueSerializer _floatSerializer;
        private readonly IValueSerializer _stringSerializer;

        public GetHumanResultParser(IValueSerializerCollection serializerResolver)
        {
            if (serializerResolver is null)
            {
                throw new ArgumentNullException(nameof(serializerResolver));
            }
            _floatSerializer = serializerResolver.Get("Float");
            _stringSerializer = serializerResolver.Get("String");
        }

        protected override IGetHuman ParserData(JsonElement data)
        {
            return new GetHuman
            (
                ParseGetHumanHuman(data, "human")
            );

        }

        private global::StrawberryShake.Client.StarWarsQuery.IHuman? ParseGetHumanHuman(
            JsonElement parent,
            string field)
        {
            if (!parent.TryGetProperty(field, out JsonElement obj))
            {
                return null;
            }

            if (obj.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return new Human
            (
                DeserializeNullableFloat(obj, "height"),
                DeserializeNullableString(obj, "name"),
                ParseGetHumanHumanFriends(obj, "friends")
            );
        }

        private global::StrawberryShake.Client.StarWarsQuery.IFriend? ParseGetHumanHumanFriends(
            JsonElement parent,
            string field)
        {
            if (!parent.TryGetProperty(field, out JsonElement obj))
            {
                return null;
            }

            if (obj.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return new Friend
            (
                ParseGetHumanHumanFriendsNodes(obj, "nodes")
            );
        }

        private global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.Client.StarWarsQuery.IHasName>? ParseGetHumanHumanFriendsNodes(
            JsonElement parent,
            string field)
        {
            if (!parent.TryGetProperty(field, out JsonElement obj))
            {
                return null;
            }

            if (obj.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            int objLength = obj.GetArrayLength();
            var list = new global::StrawberryShake.Client.StarWarsQuery.IHasName[objLength];
            for (int objIndex = 0; objIndex < objLength; objIndex++)
            {
                JsonElement element = obj[objIndex];
                list[objIndex] = new HasName
                (
                    DeserializeNullableString(element, "name")
                );

            }

            return list;
        }

        private double? DeserializeNullableFloat(JsonElement obj, string fieldName)
        {
            if (!obj.TryGetProperty(fieldName, out JsonElement value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return (double?)_floatSerializer.Deserialize(value.GetDouble())!;
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
