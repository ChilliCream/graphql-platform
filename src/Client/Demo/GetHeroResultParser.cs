using System;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake;
using StrawberryShake.Http;

namespace Foo
{
    public class GetHeroResultParser
        : GeneratedResultParserBase<IGetHero>
    {
        private readonly IValueSerializer _stringSerializer;

        public GetHeroResultParser(IEnumerable<IValueSerializer> serializers)
        {
            IReadOnlyDictionary<string, IValueSerializer> map = serializers.ToDictionary();

            if (!map.TryGetValue("String", out IValueSerializer serializer))
            {
                throw new ArgumentException(
                    "There is no serializer specified for `String`.",
                    nameof(serializers));
            }
            _stringSerializer = serializer;
        }

        protected override IGetHero ParserData(JsonElement data)
        {
            var getHero = new GetHero();
            getHero.Hero = ParseRootHero(data, "hero");
            return getHero;
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

            if (string.Equals(type, "Droid", StringComparison.Ordinal))
            {
                var droid = new Droid();
                droid.Name = (string)DeserializeStringValue(obj, "name");
                droid.Friends = ParseRootHeroFriends(obj, "friends");
                return droid;
            }

            if (string.Equals(type, "Human", StringComparison.Ordinal))
            {
                var human = new Human();
                human.Name = (string)DeserializeStringValue(obj, "name");
                human.Friends = ParseRootHeroFriends(obj, "friends");
                return human;
            }

            throw new NotSupportedException("Handle not exhausted objects");
        }

        private IFriend ParseRootHeroFriends(
            JsonElement parent,
            string field)
        {
            if (!parent.TryGetProperty(field, out JsonElement obj))
            {
                return null;
            }

            var friend = new Friend();
            friend.Nodes = ParseRootHeroFriendsNodes(obj, "nodes");
            return friend;
        }

        private IReadOnlyList<IHasName> ParseRootHeroFriendsNodes(
            JsonElement parent,
            string field)
        {
            if (!parent.TryGetProperty(field, out JsonElement obj))
            {
                return null;
            }

            string type = obj.GetProperty(TypeName).GetString();

            if (string.Equals(type, "Droid", StringComparison.Ordinal))
            {
                int objLength = obj.GetArrayLength();
                var list = new IHasName[objLength];

                for (int objIndex = 0; objIndex < objLength; objIndex++)
                {
                    JsonElement element = obj[objIndex];
                    var entity = new Droid();
                    entity.Name = (string)DeserializeStringValue(element, "name");
                    list[objIndex] = entity;
                }

                return list;
            }

            if (string.Equals(type, "Human", StringComparison.Ordinal))
            {
                int objLength = obj.GetArrayLength();
                var list = new IHasName[objLength];

                for (int objIndex = 0; objIndex < objLength; objIndex++)
                {
                    JsonElement element = obj[objIndex];
                    var entity = new Human();
                    entity.Name = (string)DeserializeStringValue(element, "name");
                    list[objIndex] = entity;
                }

                return list;
            }

            throw new NotSupportedException("Handle not exhausted objects");
        }

        private int? DeserializeNullableInt(JsonElement obj, string fieldName)
        {
            if (!obj.TryGetProperty(fieldName, out JsonElement value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return (int?)_stringSerializer.Serialize(value.GetSingle()());
        }


        private string DeserializeString(JsonElement obj, string fieldName)
        {
            if (!obj.TryGetProperty(fieldName, out JsonElement value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return (string)_stringSerializer.Serialize(value.GetString());
        }

        private List<string> DeserializeStringList(JsonElement obj, string fieldName)
        {
            if (!obj.TryGetProperty(fieldName, out JsonElement value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            int arrayLength = value.GetArrayLength();
            var list = new List<string>(arrayLength);

            for (int i = 0; i < arrayLength; i++)
            {
                if (value[i].ValueKind == JsonValueKind.Null)
                {
                    list.Add(null);
                }
                else
                {
                    list.Add((string)_stringSerializer.Serialize(value[i].GetString()));
                }
            }

            return list;
        }

        private List<List<string>> DeserializeNestedStringList(JsonElement obj, string fieldName)
        {
            if (!obj.TryGetProperty(fieldName, out JsonElement value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            int arrayLength = value.GetArrayLength();
            var list = new List<List<string>>(arrayLength);

            for (int i = 0; i < arrayLength; i++)
            {
                JsonElement element = value[i];

                if (element.ValueKind == JsonValueKind.Null)
                {
                    list.Add(null);
                }
                else
                {
                    int nestedArrayLength = element.GetArrayLength();
                    var nestedList = new List<string>(nestedArrayLength);
                    list.Add(nestedList);

                    for (int j = 0; j < nestedArrayLength; j++)
                    {
                        if (element[j].ValueKind == JsonValueKind.Null)
                        {
                            nestedList.Add(null);
                        }
                        else
                        {
                            nestedList.Add((string)_stringSerializer.Serialize(element[j].GetString()));
                        }
                    }
                }
            }

            return list;
        }

    }

}
