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
    public class SearchResultParser
        : JsonResultParserBase<ISearch>
    {
        private readonly IValueSerializer _stringSerializer;
        private readonly IValueSerializer _floatSerializer;

        public SearchResultParser(IValueSerializerResolver serializerResolver)
        {
            if (serializerResolver is null)
            {
                throw new ArgumentNullException(nameof(serializerResolver));
            }
            _stringSerializer = serializerResolver.GetValueSerializer("String");
            _floatSerializer = serializerResolver.GetValueSerializer("Float");
        }

        protected override ISearch ParserData(JsonElement data)
        {
            return new Search1
            (
                ParseSearchSearch(data, "search")
            );

        }

        private IReadOnlyList<ISearchResult>? ParseSearchSearch(
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
            var list = new ISearchResult[objLength];
            for (int objIndex = 0; objIndex < objLength; objIndex++)
            {
                JsonElement element = obj[objIndex];
                string type = element.GetProperty(TypeName).GetString();

                switch(type)
                {
                    case "Starship":
                        list[objIndex] = new Starship
                        (
                            DeserializeNullableString(element, "name")
                        );
                        break;

                    case "Human":
                        list[objIndex] = new Human
                        (
                            DeserializeNullableString(element, "name"),
                            DeserializeNullableFloat(element, "height"),
                            ParseSearchSearchFriends(element, "friends")
                        );
                        break;

                    case "Droid":
                        list[objIndex] = new Droid
                        (
                            DeserializeNullableString(element, "name"),
                            DeserializeNullableFloat(element, "height"),
                            ParseSearchSearchFriends(element, "friends")
                        );
                        break;

                    default:
                        throw new UnknownSchemaTypeException(type);
                }

            }

            return list;
        }

        private IFriend? ParseSearchSearchFriends(
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
                ParseSearchSearchFriendsNodes(obj, "nodes")
            );
        }

        private IReadOnlyList<IHasName>? ParseSearchSearchFriendsNodes(
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
