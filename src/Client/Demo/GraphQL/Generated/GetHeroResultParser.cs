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
    public class GetHeroResultParser
        : JsonResultParserBase<IGetHero>
    {
        private readonly IValueSerializer _stringSerializer;
        private readonly IValueSerializer _floatSerializer;

        public GetHeroResultParser(IValueSerializerResolver serializerResolver)
        {
            if (serializerResolver is null)
            {
                throw new ArgumentNullException(nameof(serializerResolver));
            }
            _stringSerializer = serializerResolver.GetValueSerializer("String");
            _floatSerializer = serializerResolver.GetValueSerializer("Float");
        }

        protected override IGetHero ParserData(JsonElement data)
        {
            return new GetHero
            (
                ParseGetHeroHero(data, "hero")
            );

        }

        private IHasName? ParseGetHeroHero(
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

            string type = obj.GetProperty(TypeName).GetString();

            switch(type)
            {
                case "Droid":
                    return new Droid
                    (
                        DeserializeNullableString(obj, "name"),
                        DeserializeNullableFloat(obj, "height"),
                        ParseGetHeroHeroFriends(obj, "friends")
                    );

                case "Human":
                    return new Human
                    (
                        DeserializeNullableString(obj, "name"),
                        DeserializeNullableFloat(obj, "height"),
                        ParseGetHeroHeroFriends(obj, "friends")
                    );

                default:
                    throw new UnknownSchemaTypeException(type);
            }
        }

        private IFriend? ParseGetHeroHeroFriends(
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
                ParseGetHeroHeroFriendsNodes(obj, "nodes")
            );
        }

        private IReadOnlyList<IHasName>? ParseGetHeroHeroFriendsNodes(
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
