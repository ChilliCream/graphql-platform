namespace Foo
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHeroBuilder
        : global::StrawberryShake.IOperationResultBuilder<global::System.Text.Json.JsonDocument, IGetHero>
    {
        private readonly global::StrawberryShake.IEntityStore _entityStore;
        private readonly global::System.Func<global::System.Text.Json.JsonElement, global::StrawberryShake.EntityId> _extractId;
        private readonly global::StrawberryShake.IOperationResultDataFactory<IGetHero> _resultDataFactory;
        private global::StrawberryShake.Serialization.ILeafValueParser<global::System.String, Episode> _episodeParser;
        private global::StrawberryShake.Serialization.ILeafValueParser<global::System.String, global::System.String> _stringParser;

        public GetHeroBuilder(
            global::StrawberryShake.IEntityStore entityStore,
            global::System.Func<global::System.Text.Json.JsonElement, global::StrawberryShake.EntityId> extractId,
            global::StrawberryShake.IOperationResultDataFactory<IGetHero> resultDataFactory,
            global::StrawberryShake.Serialization.ISerializerResolver serializerResolver)
        {
            _entityStore = entityStore
                 ?? throw new global::System.ArgumentNullException(nameof(entityStore));
            _extractId = extractId
                 ?? throw new global::System.ArgumentNullException(nameof(extractId));
            _resultDataFactory = resultDataFactory
                 ?? throw new global::System.ArgumentNullException(nameof(resultDataFactory));
            _episodeParser = serializerResolver.GetLeafValueParser<global::System.String, Episode>("Episode")
                 ?? throw new global::System.ArgumentNullException(nameof(_episodeParser));
            _stringParser = serializerResolver.GetLeafValueParser<global::System.String, global::System.String>("String")
                 ?? throw new global::System.ArgumentNullException(nameof(_stringParser));
        }

        public global::StrawberryShake.IOperationResult<IGetHero> Build(global::StrawberryShake.Response<global::System.Text.Json.JsonDocument> response)
        {
            (IGetHero Result, GetHeroInfo Info)? data = null;

            if (response.Body is not null
                && response.Body.RootElement.TryGetProperty("data", out global::System.Text.Json.JsonElement obj))
            {
                data = BuildData(obj);
            }

            return new global::StrawberryShake.OperationResult<IGetHero>(
                data?.Result,
                data?.Info,
                _resultDataFactory,
                null
            );
        }

        private (IGetHero, GetHeroInfo) BuildData(global::System.Text.Json.JsonElement obj)
        {
            using global::StrawberryShake.IEntityUpdateSession session = _entityStore.BeginUpdate();
            var entityIds = new global::System.Collections.Generic.HashSet<global::StrawberryShake.EntityId>();

            global::StrawberryShake.EntityId? heroId = UpdateIGetHero_HeroEntity(
                global::StrawberryShake.Transport.Http.JsonElementExtensions.GetPropertyOrNull(obj, "hero"),
                entityIds
            );

            var resultInfo = new GetHeroInfo(
                heroId,
                entityIds,
                session.Version
            );

            return (_resultDataFactory.Create(resultInfo), resultInfo);
        }

        private global::StrawberryShake.EntityId? UpdateIGetHero_HeroEntity(
            global::System.Text.Json.JsonElement? obj,
            global::System.Collections.Generic.ISet<global::StrawberryShake.EntityId> entityIds)
        {
            if (!obj.HasValue)
            {
                return null;
            }

            global::StrawberryShake.EntityId entityId = _extractId(obj.Value);
            entityIds.Add(entityId);


            if (entityId.Name.Equals("GetHero_Hero_Droid", global::System.StringComparison.Ordinal))
            {
                DroidEntity entity = _entityStore.GetOrCreate<DroidEntity>(entityId);
                entity.Name = DeserializeNonNullableString(global::StrawberryShake.Transport.Http.JsonElementExtensions.GetPropertyOrNull(obj.Value, "name"));
                entity.AppearsIn = DeserializeEpisodeArray(global::StrawberryShake.Transport.Http.JsonElementExtensions.GetPropertyOrNull(obj.Value, "appearsIn"));

                return entityId;
            }

            if (entityId.Name.Equals("GetHero_Hero_Human", global::System.StringComparison.Ordinal))
            {
                HumanEntity entity = _entityStore.GetOrCreate<HumanEntity>(entityId);
                entity.Name = DeserializeNonNullableString(global::StrawberryShake.Transport.Http.JsonElementExtensions.GetPropertyOrNull(obj.Value, "name"));
                entity.AppearsIn = DeserializeEpisodeArray(global::StrawberryShake.Transport.Http.JsonElementExtensions.GetPropertyOrNull(obj.Value, "appearsIn"));

                return entityId;
            }

            throw new global::System.NotSupportedException();
        }

        private global::System.String DeserializeNonNullableString(global::System.Text.Json.JsonElement? obj)
        {
            if (!obj.HasValue)
            {
                throw new global::System.ArgumentNullException();
            }

            return _stringParser.Parse(obj.Value.GetString()!);
        }

        private global::System.Collections.Generic.IReadOnlyList<Episode?>? DeserializeEpisodeArray(global::System.Text.Json.JsonElement? obj)
        {
            if (!obj.HasValue)
            {
                return null;
            }

            var episodes = new global::System.Collections.Generic.List<Episode?>();

            foreach (global::System.Text.Json.JsonElement child in obj.Value.EnumerateArray())
            {
                episodes.Add(DeserializeEpisode(child));
            }

            return episodes;
        }

        private Episode? DeserializeEpisode(global::System.Text.Json.JsonElement? obj)
        {
            if (!obj.HasValue)
            {
                return null;
            }

            return _episodeParser.Parse(obj.Value.GetString()!);
        }
    }
}
