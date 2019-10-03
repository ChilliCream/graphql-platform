using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake;
using StrawberryShake.Http;

namespace  StrawberryShake.Client.GraphQL
{
    public class GetHumanResultParser
        : JsonResultParserBase<IGetHuman>
    {
        private readonly IValueSerializer _stringSerializer;
        private readonly IValueSerializer _floatSerializer;

        public GetHumanResultParser(IEnumerable<IValueSerializer> serializers)
        {
            IReadOnlyDictionary<string, IValueSerializer> map = serializers.ToDictionary();

            if (!map.TryGetValue("String", out IValueSerializer? serializer))
            {
                throw new ArgumentException(
                    "There is no serializer specified for `String`.",
                    nameof(serializers));
            }
            _stringSerializer = serializer;

            if (!map.TryGetValue("Float", out  serializer))
            {
                throw new ArgumentException(
                    "There is no serializer specified for `Float`.",
                    nameof(serializers));
            }
            _floatSerializer = serializer;
        }

        private IHero? ParseRootHuman(
            JsonElement parent,
            string field)
        {
            if (!parent.TryGetProperty(field, out JsonElement obj))
            {
                return null;
            }

            return new Hero
            (
                DeserializeNullableString(obj, "name"),
                DeserializeNullableFloat(obj, "height"),
                ParseRootHumanFriends(obj, "friends")
            );
        }

        private IFriend? ParseRootHumanFriends(
            JsonElement parent,
            string field)
        {
            if (!parent.TryGetProperty(field, out JsonElement obj))
            {
                return null;
            }

            return new Friend
            (
                ParseRootHumanFriendsNodes(obj, "nodes")
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

            return (string?)_stringSerializer.Serialize(value.GetString())!;
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

            return (double?)_floatSerializer.Serialize(value.GetDouble())!;
        }
    }
}
