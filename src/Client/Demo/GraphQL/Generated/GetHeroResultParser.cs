using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake;
using StrawberryShake.Http;

namespace Foo
{
    public class GetHeroResultParser
        : GeneratedResultParserBase<IGetHero>
    {
        private readonly IValueSerializer _floatSerializer;
        private readonly IValueSerializer _stringSerializer;

        public GetHeroResultParser(IEnumerable<IValueSerializer> serializers)
        {
            IReadOnlyDictionary<string, IValueSerializer> map = serializers.ToDictionary();

            if (!map.TryGetValue("Float", out IValueSerializer serializer)){
                throw new ArgumentException(
                    "There is no serializer specified for `Float`.",
                    nameof(serializers));
            }
            _floatSerializer = serializer;

            if (!map.TryGetValue("String", out  serializer)){
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
                droid.Height = (double?)DeserializeFloat(obj, "height");
                droid.Name = (string)DeserializeString(obj, "name");
                droid.Friends = ParseRootHeroFriends(obj, "friends");
                return droid;
            }

            if (string.Equals(type, "Human", StringComparison.Ordinal))
            {
                var human = new Human();
                human.Height = (double?)DeserializeFloat(obj, "height");
                human.Name = (string)DeserializeString(obj, "name");
                human.Friends = ParseRootHeroFriends(obj, "friends");
                return human;
            }

            throw new UnknownSchemaTypeException(type);
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
                    entity.Name = (string)DeserializeString(element, "name");
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
                    entity.Name = (string)DeserializeString(element, "name");
                    list[objIndex] = entity;
                }

                return list;
            }

            throw new UnknownSchemaTypeException(type);
        }

        private double? DeserializeFloat(JsonElement obj, string fieldName)
        {
            if (!obj.TryGetProperty(fieldName, out JsonElement value))
            {
                return null;
            }

            return (double?)_floatSerializer.Serialize(value.GetDouble());
        }
        private string DeserializeString(JsonElement obj, string fieldName)
        {
            if (!obj.TryGetProperty(fieldName, out JsonElement value))
            {
                return null;
            }

            return (string)_stringSerializer.Serialize(value.GetString());
        }
    }
}
