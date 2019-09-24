using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake;
using StrawberryShake.Http;

namespace StrawberryShake.Client
{
    public class GetHeroResultParser
        : JsonResultParserBase<IGetHero>
    {
        private readonly IValueSerializer _floatSerializer;
        private readonly IValueSerializer _stringSerializer;

        public GetHeroResultParser(IEnumerable<IValueSerializer> serializers)
        {
            IReadOnlyDictionary<string, IValueSerializer> map = serializers.ToDictionary();

            if (!map.TryGetValue("Float", out IValueSerializer? serializer))
            {
                throw new ArgumentException(
                    "There is no serializer specified for `Float`.",
                    nameof(serializers));
            }
            _floatSerializer = serializer;

            if (!map.TryGetValue("String", out  serializer))
            {
                throw new ArgumentException(
                    "There is no serializer specified for `String`.",
                    nameof(serializers));
            }
            _stringSerializer = serializer;
        }

        protected override IGetHero ParserData(JsonElement data)
        {
            return new GetHero
            (
                ParseRootHero(data, "hero")
            );

        }

        private IHero ParseRootHero(
            JsonElement parent,
            string field)
        {
            if (!parent.TryGetProperty(field, out JsonElement obj))
            {
                return null;
            }

            string type = obj.GetProperty(TypeName).GetString();

            switch(type)
            {
                case "Hero":
                    return new Hero
                    (
                        DeserializeNullableFloat(obj, "height"),
                        DeserializeNullableString(obj, "name"),
                        ParseRootHeroFriends(obj, "friends")
                    );

                default:
                    throw new UnknownSchemaTypeException(type);
            }
        }

        private IFriend ParseRootHeroFriends(
            JsonElement parent,
            string field)
        {
            if (!parent.TryGetProperty(field, out JsonElement obj))
            {
                return null;
            }

            return new Friend
            (
                ParseRootHeroFriendsNodes(obj, "nodes")
            );

        }

        private IReadOnlyList<IHasName> ParseRootHeroFriendsNodes(
            JsonElement parent,
            string field)
        {
            if (!parent.TryGetProperty(field, out JsonElement obj))
            {
                return null;
            }

            int objLength = obj.GetArrayLength();
            var list = new IHasName[objLength];
            for (int objIndex = 0; objIndex < objLength; objIndex++)
            {
                JsonElement element = obj[objIndex];
                string type = obj.GetProperty(TypeName).GetString();

                switch(type)
                {
                    case "HasName":
                        list[objIndex] = new HasName
                        (
                            DeserializeNullableString(obj, "name")
                        );
                        break;

                    default:
                        throw new UnknownSchemaTypeException(type);
                }

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


            return (double?)_floatSerializer.Serialize(value.GetDouble());
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


            return (string?)_stringSerializer.Serialize(value.GetString());
        }
    }
}
