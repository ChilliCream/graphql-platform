using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake;
using StrawberryShake.Http;

namespace StrawberryShake.Client.GraphQL
{
    public class GetCharacterResultParser
        : JsonResultParserBase<IGetCharacter>
    {
        private readonly IValueSerializer _stringSerializer;
        private readonly IValueSerializer _floatSerializer;

        public GetCharacterResultParser(IEnumerable<IValueSerializer> serializers)
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

        protected override IGetCharacter ParserData(JsonElement data)
        {
            return new GetCharacter
            (
                ParseGetCharacterCharacter(data, "character")
            );

        }

        private IReadOnlyList<IHasName> ParseGetCharacterCharacter(
            JsonElement parent,
            string field)
        {
            JsonElement obj = parent.GetProperty(field);

            int objLength = obj.GetArrayLength();
            var list = new IHasName[objLength];
            for (int objIndex = 0; objIndex < objLength; objIndex++)
            {
                JsonElement element = obj[objIndex];
                string type = element.GetProperty(TypeName).GetString();

                switch(type)
                {
                    case "Droid":
                        list[objIndex] = new Droid
                        (
                            DeserializeNullableString(element, "name"),
                            DeserializeNullableFloat(element, "height"),
                            ParseGetCharacterCharacterFriends(element, "friends")
                        );
                        break;

                    case "Human":
                        list[objIndex] = new Human
                        (
                            DeserializeNullableString(element, "name"),
                            DeserializeNullableFloat(element, "height"),
                            ParseGetCharacterCharacterFriends(element, "friends")
                        );
                        break;

                    default:
                        throw new UnknownSchemaTypeException(type);
                }

            }

            return list;
        }

        private IFriend? ParseGetCharacterCharacterFriends(
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
                ParseGetCharacterCharacterFriendsNodes(obj, "nodes")
            );
        }

        private IReadOnlyList<IHasName>? ParseGetCharacterCharacterFriendsNodes(
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
            var list = new IHasName[objLength];
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
    }
}
