namespace Foo
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHeroFactory
        : global::StrawberryShake.IOperationResultDataFactory<GetHero>
    {
        private readonly global::StrawberryShake.IEntityStore _entityStore;
        private readonly global::StrawberryShake.IEntityMapper<DroidEntity, GetHero_Hero_Droid> _getHero_Hero_DroidFromDroidEntityMapper;
        private readonly global::StrawberryShake.IEntityMapper<HumanEntity, GetHero_Hero_Human> _getHero_Hero_HumanFromHumanEntityMapper;

        public GetHeroFactory(
            global::StrawberryShake.IEntityStore entityStore,
            global::StrawberryShake.IEntityMapper<DroidEntity, GetHero_Hero_Droid> getHero_Hero_DroidFromDroidEntityMapper,
            global::StrawberryShake.IEntityMapper<HumanEntity, GetHero_Hero_Human> getHero_Hero_HumanFromHumanEntityMapper)
        {
            _entityStore = entityStore
                 ?? throw new global::System.ArgumentNullException(nameof(entityStore));
            _getHero_Hero_DroidFromDroidEntityMapper = getHero_Hero_DroidFromDroidEntityMapper
                 ?? throw new global::System.ArgumentNullException(nameof(getHero_Hero_DroidFromDroidEntityMapper));
            _getHero_Hero_HumanFromHumanEntityMapper = getHero_Hero_HumanFromHumanEntityMapper
                 ?? throw new global::System.ArgumentNullException(nameof(getHero_Hero_HumanFromHumanEntityMapper));
        }

        public GetHero Create(global::StrawberryShake.IOperationResultDataInfo dataInfo)
        {
            if (dataInfo is GetHeroInfo info)
            {
                IGetHero_Hero hero = default!;

                var heroInfo = info.Hero ?? throw new global::System.ArgumentNullException();
                if (heroInfo.Name.Equals(
                    "Droid",
                    global::System.StringComparison.Ordinal
                ))
                {
                    hero = _getHero_Hero_DroidFromDroidEntityMapper.Map(_entityStore.GetEntity<DroidEntity>(heroInfo) ?? throw new global::System.ArgumentNullException());
                }

                if (heroInfo.Name.Equals(
                    "Human",
                    global::System.StringComparison.Ordinal
                ))
                {
                    hero = _getHero_Hero_HumanFromHumanEntityMapper.Map(_entityStore.GetEntity<HumanEntity>(heroInfo) ?? throw new global::System.ArgumentNullException());
                }

                return new GetHero(hero);
            }

            throw new global::System.ArgumentException("GetHeroInfo expected.");
        }
    }
}
