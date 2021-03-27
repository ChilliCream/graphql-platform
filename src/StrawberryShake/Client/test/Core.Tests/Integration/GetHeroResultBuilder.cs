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
            var entityIds = new HashSet<EntityId>();
            IEntityStoreSnapshot snapshot = default!;
            EntityId heroId = default!;

            // store updates ...
            _entityStore.Update(session =>
            {
                heroId = UpdateHeroEntity(session, obj.GetProperty("hero"), entityIds);
                snapshot = session.CurrentSnapshot;
            });

            // build result
            var resultInfo = new GetHeroResultInfo(
                heroId,
                DeserializeNonNullString(obj, "version"),
                entityIds,
                snapshot.Version);

            return (_resultDataFactory.Create(resultInfo, snapshot), resultInfo);
        }

        private EntityId UpdateHeroEntity(
            IEntityStoreUpdateSession session,
            JsonElement obj,
            ISet<EntityId> entityIds)
        {
            EntityId entityId = _extractId(obj);
            entityIds.Add(entityId);

            if (entityId.Name.Equals("Human", StringComparison.Ordinal))
            {
                var friends = new List<EntityId>();

                foreach (JsonElement child in obj.GetProperty("friends").EnumerateArray())
                {
                    friends.Add(UpdateFriendEntity(session, child, entityIds));
                }

                if (session.CurrentSnapshot.TryGetEntity(entityId, out HumanEntity? entity))
                {
                    // in case we update we will copy over fields that we do not know and insert
                    // the ones that are from this request.
                    entity = new HumanEntity(
                        entity.Id,
                        DeserializeNonNullString(obj, "name"),
                        friends);
                }
                else
                {
                    // in the case we need to create the entity we will default things we
                    // do not know.
                    entity = new HumanEntity(
                        default!,
                        DeserializeNonNullString(obj, "name"),
                        friends);
                }

                return entityId;
            }

            if (entityId.Name.Equals("Droid", StringComparison.Ordinal))
            {
                var friends = new List<EntityId>();

                foreach (JsonElement child in obj.GetProperty("friends").EnumerateArray())
                {
                    friends.Add(UpdateFriendEntity(session, child, entityIds));
                }

                if (session.CurrentSnapshot.TryGetEntity(entityId, out DroidEntity? entity))
                {
                    // in case we update we will copy over fields that we do not know and insert
                    // the ones that are from this request.
                    entity = new DroidEntity(
                        entity.Id,
                        DeserializeNonNullString(obj, "name"),
                        friends);
                }
                else
                {
                    // in the case we need to create the entity we will default things we
                    // do not know.
                    entity = new DroidEntity(
                        default!,
                        DeserializeNonNullString(obj, "name"),
                        friends);
                }

                return entityId;
            }

            throw new NotSupportedException();
        }

        private EntityId UpdateFriendEntity(
            IEntityStoreUpdateSession session,
            JsonElement obj,
            ISet<EntityId> entityIds)
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
            if (obj.TryGetProperty(propertyName, out JsonElement property) &&
                property.ValueKind != JsonValueKind.Null)
            {
                return _stringParser.Parse(property.GetString()!);
            }

            throw new InvalidOperationException();
        }
    }
}
