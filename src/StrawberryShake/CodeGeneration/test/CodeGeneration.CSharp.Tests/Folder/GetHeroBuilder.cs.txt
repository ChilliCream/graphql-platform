using System;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake;
using StrawberryShake.Json;
using StrawberryShake.Serialization;

namespace Foo.Bar.State;

[System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
public sealed partial class GetHeroBuilder : OperationResultBuilder<IGetHeroResult>
{
    private readonly IEntityStore _entityStore;
    private readonly IEntityIdSerializer _idSerializer;
    private readonly ILeafValueParser<string, Episode> _episodeParser;
    private readonly ILeafValueParser<string, string> _stringParser;

    public GetHeroBuilder(
        IEntityStore entityStore,
        IEntityIdSerializer idSerializer,
        IOperationResultDataFactory<IGetHeroResult> resultDataFactory,
        ISerializerResolver serializerResolver)
    {
        _entityStore = entityStore ?? throw new ArgumentNullException(nameof(entityStore));
        _idSerializer = idSerializer ?? throw new ArgumentNullException(nameof(idSerializer));
        ResultDataFactory = resultDataFactory ?? throw new ArgumentNullException(nameof(resultDataFactory));
        _episodeParser = serializerResolver.GetLeafValueParser<string, Episode>("Episode") ?? throw new ArgumentException("No serializer for type `Episode` found.");
        _stringParser = serializerResolver.GetLeafValueParser<string, string>("String") ?? throw new ArgumentException("No serializer for type `String` found.");
    }

    protected override IOperationResultDataFactory<IGetHeroResult> ResultDataFactory { get; }

    protected override IOperationResultDataInfo BuildData(JsonElement dataProp)
    {
        var entityIds = new HashSet<EntityId>();
        var pathToEntityId = new Dictionary<string, EntityId>();

        IEntityStoreSnapshot snapshot = default!;
        EntityId? heroId = default!;

        _entityStore.Update(
            session =>
            {
                heroId = Update_GetHero_HeroEntity(
                    session,
                    dataProp.GetPropertyOrNull("hero"),
                    entityIds,
                    pathToEntityId,
                    "/hero");
                snapshot = session.CurrentSnapshot;
            });

        return new GetHeroResultInfo(
            heroId,
            entityIds,
            pathToEntityId,
            snapshot.Version);
    }

    protected override IOperationResultDataInfo PatchData(PatchContext context)
    {
        IEntityStoreSnapshot snapshot = default!;
        EntityId? heroId = default!;

        _entityStore.Update(
            session =>
            {
                PatchAppearsIn(
                    session,
                    context.DataProp,
                    context.EntityIds,
                    context.PathToEntityIds,
                    context.Path);

                snapshot = session.CurrentSnapshot;
            });

        return new GetHeroResultInfo(
            heroId,
            context.EntityIds,
            context.PathToEntityIds,
            snapshot.Version);
    }

    private void PatchAppearsIn(
        IEntityStoreUpdateSession session,
        JsonElement? obj,
        ISet<EntityId> entityIds,
        Dictionary<string, EntityId> pathToEntityId,
        string path)
    {
        if (obj.HasValue &&
            obj.Value.TryGetProperty("_isHeroAppearsInFulfilled", out _) &&
            pathToEntityId.TryGetValue(path, out EntityId entityId) &&
            session.CurrentSnapshot.TryGetEntity(entityId, out DroidEntity? entity))
        {
            session.SetEntity(
                entityId,
                new DroidEntity(
                    entity.Name,
                    entity.Friends,
                    true,
                    DeserializeAppearsIn(obj.Value.GetPropertyOrNull("appearsIn")),
                    entity.IsHeroAppearsIn2Fulfilled));
        }
    }

    private EntityId? Update_GetHero_HeroEntity(
        IEntityStoreUpdateSession session,
        JsonElement? obj,
        ISet<EntityId> entityIds,
        Dictionary<string, EntityId> pathToEntityId,
        string path)
    {
        if (!obj.HasValue)
        {
            return null;
        }

        EntityId entityId = _idSerializer.Parse(obj.Value);
        entityIds.Add(entityId);
        pathToEntityId[path] = entityId;

        if (entityId.Name.Equals("Droid", StringComparison.Ordinal))
        {
            if (session.CurrentSnapshot.TryGetEntity(entityId, out DroidEntity? entity))
            {
                session.SetEntity(
                    entityId,
                    new DroidEntity(
                        DeserializeNonNullableString(obj.GetPropertyOrNull("name")),
                        Deserialize_GetHero_Hero_Friends(session, obj.GetPropertyOrNull("friends"), entityIds),
                        entity.IsHeroAppearsInFulfilled,
                        entity.AppearsIn,
                        entity.IsHeroAppearsIn2Fulfilled));
            }
            else
            {
                session.SetEntity(
                    entityId,
                    new DroidEntity(
                        DeserializeNonNullableString(obj.GetPropertyOrNull("name")),
                        Deserialize_GetHero_Hero_Friends(session, obj.GetPropertyOrNull("friends"), entityIds),
                        default!,
                        default!,
                        default!));
            }

            PatchAppearsIn(session, obj, entityIds, pathToEntityId, path);

            return entityId;
        }

        if (entityId.Name.Equals("Human", StringComparison.Ordinal))
        {
            if (session.CurrentSnapshot.TryGetEntity(entityId, out HumanEntity? entity))
            {
                session.SetEntity(
                    entityId,
                    new HumanEntity(
                        DeserializeNonNullableString(obj.GetPropertyOrNull("name")),
                        Deserialize_GetHero_Hero_Friends(session, obj.GetPropertyOrNull("friends"), entityIds),
                        entity.IsHeroAppearsInFulfilled,
                        entity.AppearsIn,
                        entity.IsHeroAppearsIn2Fulfilled));
            }
            else
            {
                session.SetEntity(
                    entityId,
                    new HumanEntity(
                        DeserializeNonNullableString(obj.GetPropertyOrNull("name")),
                        Deserialize_GetHero_Hero_Friends(session, obj.GetPropertyOrNull("friends"), entityIds),
                        default!,
                        default!,
                        default!));
            }

            return entityId;
        }

        throw new NotSupportedException();
    }

    private string DeserializeNonNullableString(JsonElement? obj)
    {
        if (!obj.HasValue)
        {
            throw new ArgumentNullException();
        }

        return _stringParser.Parse(obj.Value.GetString()!);
    }

    private List<Episode?> DeserializeAppearsIn(JsonElement? obj)
        => throw new NotImplementedException();

    private FriendsConnectionData? Deserialize_GetHero_Hero_Friends(
        IEntityStoreUpdateSession session,
        JsonElement? obj,
        ISet<EntityId> entityIds)
    {
        if (!obj.HasValue)
        {
            return null;
        }

        var typename = obj.Value.GetProperty("__typename").GetString();

        if (typename?.Equals("FriendsConnection", StringComparison.Ordinal) ?? false)
        {
            return new FriendsConnectionData(
                typename,
                Update_GetHero_Hero_Friends_NodesEntityArray(
                    session,
                    obj.GetPropertyOrNull("nodes"),
                    entityIds));
        }

        throw new NotSupportedException();
    }

    private IReadOnlyList<EntityId?>? Update_GetHero_Hero_Friends_NodesEntityArray(
        IEntityStoreUpdateSession session,
        JsonElement? obj,
        ISet<EntityId> entityIds)
    {
        if (!obj.HasValue)
        {
            return null;
        }

        var characters = new List<EntityId?>();

        foreach (JsonElement child in obj.Value.EnumerateArray())
        {
            characters.Add(Update_GetHero_Hero_Friends_NodesEntity(session, child, entityIds));
        }

        return characters;
    }

    private EntityId? Update_GetHero_Hero_Friends_NodesEntity(
        IEntityStoreUpdateSession session,
        JsonElement? obj,
        ISet<EntityId> entityIds)
    {
        if (!obj.HasValue)
        {
            return null;
        }

        EntityId entityId = _idSerializer.Parse(obj.Value);
        entityIds.Add(entityId);

        if (entityId.Name.Equals("Droid", StringComparison.Ordinal))
        {
            if (session.CurrentSnapshot.TryGetEntity(entityId, out DroidEntity? entity))
            {
                session.SetEntity(
                    entityId,
                    new DroidEntity(
                        DeserializeNonNullableString(obj.GetPropertyOrNull("name")),
                        entity.Friends,
                        entity.IsHeroAppearsInFulfilled,
                        entity.AppearsIn,
                        entity.IsHeroAppearsIn2Fulfilled));
            }
            else
            {
                session.SetEntity(
                    entityId,
                    new DroidEntity(
                        DeserializeNonNullableString(obj.GetPropertyOrNull("name")),
                        default!,
                        default!,
                        default!,
                        default!));
            }

            return entityId;
        }

        if (entityId.Name.Equals("Human", StringComparison.Ordinal))
        {
            if (session.CurrentSnapshot.TryGetEntity(entityId, out HumanEntity? entity))
            {
                session.SetEntity(
                    entityId,
                    new HumanEntity(
                        DeserializeNonNullableString(obj.GetPropertyOrNull("name")),
                        entity.Friends,
                        entity.IsHeroAppearsInFulfilled,
                        entity.AppearsIn,
                        entity.IsHeroAppearsIn2Fulfilled));
            }
            else
            {
                session.SetEntity(
                    entityId,
                    new HumanEntity(
                        DeserializeNonNullableString(obj.GetPropertyOrNull("name")),
                        default!,
                        default!,
                        default!,
                        default!));
            }

            return entityId;
        }

        throw new NotSupportedException();
    }
}
