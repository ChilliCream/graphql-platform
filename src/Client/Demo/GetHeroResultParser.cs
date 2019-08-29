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

            if (!map.TryGetValue("String", out IValueSerializer serializer)){
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

}

}
