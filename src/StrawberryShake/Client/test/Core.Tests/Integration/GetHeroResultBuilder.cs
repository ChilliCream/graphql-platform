using System;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake.Serialization;

namespace StrawberryShake.Integration
{
    public class GetHeroResultBuilder : IOperationResultBuilder<JsonDocument, GetHeroResult>
    {
        private readonly IEntityStore _entityStore;
        private readonly Func<JsonElement, EntityId> _extractId;
        private readonly IOperationResultDataFactory<GetHeroResult> _resultDataFactory;
        private readonly ILeafValueParser<string, string> _stringParser;

        public GetHeroResultBuilder(
            IEntityStore entityStore,
            Func<JsonElement, EntityId> extractId,
            IOperationResultDataFactory<GetHeroResult> resultDataFactory,
            ISerializerResolver stringParser)
        {
            _entityStore = entityStore;
            _extractId = extractId;
            _resultDataFactory = resultDataFactory;
            _stringParser = stringParser.GetLeafValueParser<string, string>("String");
        }

        public IOperationResult<GetHeroResult> Build(Response<JsonDocument> response)
        {
            (GetHeroResult Result, GetHeroResultInfo Info)? data = null;

            if (response.Body is not null &&
                response.Body.RootElement.TryGetProperty("data", out JsonElement obj))
            {
                data = BuildData(obj);
            }

            return new OperationResult<GetHeroResult>(
                data?.Result,
                data?.Info,
                _resultDataFactory,
                null);
        }

        private (GetHeroResult, GetHeroResultInfo) BuildData(JsonElement obj)
        {
            using IEntityUpdateSession session = _entityStore.BeginUpdate();
            var entityIds = new HashSet<EntityId>();

            // store updates ...
            EntityId heroId = UpdateHeroEntity(obj.GetProperty("hero"), entityIds);

            // build result
            var resultInfo = new GetHeroResultInfo(
                heroId,
                DeserializeNonNullString(obj, "version"),
                entityIds,
                session.Version);

            return (_resultDataFactory.Create(resultInfo), resultInfo);
        }

        private EntityId UpdateHeroEntity(JsonElement obj, ISet<EntityId> entityIds)
        {
            EntityId entityId = _extractId(obj);
            entityIds.Add(entityId);

            if (entityId.Name.Equals("Human", StringComparison.Ordinal))
            {
                HumanEntity entity = _entityStore.GetOrCreate<HumanEntity>(entityId);
                entity.Name = DeserializeNonNullString(obj, "name");

                var friends = new List<EntityId>();

                foreach (JsonElement child in obj.GetProperty("friends").EnumerateArray())
                {
                    friends.Add(UpdateFriendEntity(child, entityIds));
                }

                entity.Friends = friends;
                return entityId;
            }

            if (entityId.Name.Equals("Droid", StringComparison.Ordinal))
            {
                DroidEntity entity = _entityStore.GetOrCreate<DroidEntity>(entityId);
                entity.Name = DeserializeNonNullString(obj, "name");

                var friends = new List<EntityId>();

                foreach (JsonElement child in obj.GetProperty("friends").EnumerateArray())
                {
                    friends.Add(UpdateFriendEntity(child, entityIds));
                }

                entity.Friends = friends;

                return entityId;
            }

            throw new NotSupportedException();
        }

        private EntityId UpdateFriendEntity(JsonElement obj, ISet<EntityId> entityIds)
        {
            EntityId entityId = _extractId(obj);
            entityIds.Add(entityId);

            if (entityId.Name.Equals("Human", StringComparison.Ordinal))
            {
                HumanEntity entity = _entityStore.GetOrCreate<HumanEntity>(entityId);
                entity.Name = DeserializeNonNullString(obj, "name");
                return entityId;
            }

            if (entityId.Name.Equals("Droid", StringComparison.Ordinal))
            {
                DroidEntity entity = _entityStore.GetOrCreate<DroidEntity>(entityId);
                entity.Name = DeserializeNonNullString(obj, "name");
                return entityId;
            }

            throw new NotSupportedException();
        }

        private string DeserializeNonNullString(JsonElement obj, string propertyName)
        {
            if (obj.GetProperty(propertyName, out JsonElement property) &&
                property.ValueKind != JsonValueKind.Null)
            {
                return _stringParser.Parse(property.GetString()!);
            }

            throw new InvalidOperationException();
        }
    }
}
