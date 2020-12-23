using System;

namespace StrawberryShake.Remove
{
    public class GerHeroResultFactory : IOperationResultDataFactory<GetHeroResult>
    {
        private readonly IEntityStore _entityStore;
        private readonly IEntityMapper<HumanEntity, HumanHero> _humanHeroMapper;
        private readonly IEntityMapper<DroidEntity, DroidHero> _droidHeroMapper;

        public GerHeroResultFactory(
            IEntityStore entityStore,
            IEntityMapper<HumanEntity, HumanHero> humanHeroMapper,
            IEntityMapper<DroidEntity, DroidHero> droidHeroMapper)
        {
            _entityStore = entityStore ??
                throw new ArgumentNullException(nameof(entityStore));
            _humanHeroMapper = humanHeroMapper ??
                throw new ArgumentNullException(nameof(humanHeroMapper));
            _droidHeroMapper = droidHeroMapper ??
                throw new ArgumentNullException(nameof(droidHeroMapper));
        }

        public GetHeroResult Create(IOperationResultDataInfo resultInfo)
        {
            if (resultInfo is GetHeroResultInfo info)
            {
                IHero hero = default!;

                if (info.HeroId.Name.Equals("Human", StringComparison.Ordinal))
                {
                    hero = _humanHeroMapper.Map(
                        _entityStore.GetEntity<HumanEntity>(info.HeroId)!);
                }

                if (info.HeroId.Name.Equals("Droid", StringComparison.Ordinal))
                {
                    hero = _droidHeroMapper.Map(
                        _entityStore.GetEntity<DroidEntity>(info.HeroId)!);
                }

                return new GetHeroResult(
                    hero,
                    info.Version);
            }

            throw new ArgumentException("GetHeroResultInfo expected.");
        }
    }
}
