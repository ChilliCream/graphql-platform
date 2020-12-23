using System;
using System.Collections.Generic;
using System.Text.Json;

namespace StrawberryShake.Remove
{
    public class GetHeroResultBuilder
        : IOperationResultBuilder<JsonDocument, GetHeroResult>
    {
        private readonly IEntityStore _entityStore;
        private readonly Func<JsonElement, EntityId> _extractId;
        private readonly IEntityMapper<HumanEntity, HumanHero> _humanHeroMapper;
        private readonly IEntityMapper<DroidEntity, DroidHero> _droidHeroMapper;
        private readonly IValueSerializer<string, string> _stringSerializer;

        public GetHeroResultBuilder(
            IEntityStore entityStore,
            Func<JsonElement, EntityId> extractId,
            IEntityMapper<HumanEntity, HumanHero> humanHeroMapper,
            IEntityMapper<DroidEntity, DroidHero> droidHeroMapper,
            IValueSerializerResolver valueSerializerResolver)
        {
            _entityStore = entityStore;
            _extractId = extractId;
            _humanHeroMapper = humanHeroMapper;
            _droidHeroMapper = droidHeroMapper;
            _stringSerializer = valueSerializerResolver.GetValueSerializer<string, string>("String");
        }

        public IOperationResult<GetHeroResult> Build(Response<JsonDocument> response)
        {
            GetHeroResult? data = null;

            if (response.Body is not null &&
                response.Body.RootElement.TryGetProperty("data", out JsonElement obj))
            {
                data = BuildDataFromJson(obj);
            }

            return new OperationResult<GetHeroResult>(
                data,
                null);
        }

        private GetHeroResult BuildDataFromJson(JsonElement obj)
        {
            // store updates ...
            EntityId heroId;

            using (_entityStore.BeginUpdate())
            {
                heroId = UpdateHeroEntity(obj.GetProperty("hero"));
            }

            // create result
            IHero hero = default!;

            if (heroId.Name.Equals("Human", StringComparison.Ordinal))
            {
                hero = _humanHeroMapper.Map(_entityStore.GetEntity<HumanEntity>(heroId)!);
            }

            if (heroId.Name.Equals("Droid", StringComparison.Ordinal))
            {
                hero = _droidHeroMapper.Map(_entityStore.GetEntity<DroidEntity>(heroId)!);
            }

            return new GetHeroResult(
                hero,
                DeserializeNonNullString(obj, "version"));
        }

        private EntityId UpdateHeroEntity(JsonElement obj)
        {
            EntityId entityId = _extractId(obj);

            if (entityId.Name.Equals("Human", StringComparison.Ordinal))
            {
                HumanEntity entity = _entityStore.GetOrCreate<HumanEntity>(entityId);
                entity.Name = DeserializeNonNullString(obj, "name");

                var friends = new List<EntityId>();

                foreach (JsonElement child in obj.GetProperty("friends").EnumerateArray())
                {
                    friends.Add(UpdateFriendEntity(child));
                }

                entity.Friends = friends;
            }

            if (entityId.Name.Equals("Droid", StringComparison.Ordinal))
            {
                DroidEntity entity = _entityStore.GetOrCreate<DroidEntity>(entityId);
                entity.Name = DeserializeNonNullString(obj, "name");

                var friends = new List<EntityId>();

                foreach (JsonElement child in obj.GetProperty("friends").EnumerateArray())
                {
                    friends.Add(UpdateFriendEntity(child));
                }

                entity.Friends = friends;
            }

            throw new NotSupportedException();
        }

        private EntityId UpdateFriendEntity(JsonElement obj)
        {
            EntityId entityId = _extractId(obj);

            if (entityId.Name.Equals("Human", StringComparison.Ordinal))
            {
                HumanEntity entity = _entityStore.GetOrCreate<HumanEntity>(entityId);
                entity.Name = DeserializeNonNullString(obj, "name");
            }

            if (entityId.Name.Equals("Droid", StringComparison.Ordinal))
            {
                DroidEntity entity = _entityStore.GetOrCreate<DroidEntity>(entityId);
                entity.Name = DeserializeNonNullString(obj, "name");
            }

            throw new NotSupportedException();
        }

        private string DeserializeNonNullString(JsonElement obj, string propertyName)
        {
            if (obj.TryGetProperty(propertyName, out JsonElement property))
            {
                _stringSerializer.Deserialize(property.GetString());
            }

            throw new InvalidOperationException();
        }
    }
}
