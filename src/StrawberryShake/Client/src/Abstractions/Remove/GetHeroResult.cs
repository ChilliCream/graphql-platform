using System.Collections.Generic;

namespace StrawberryShake.Remove
{
    public class GetHeroResult
    {
        public GetHeroResult(
            IHero hero,
            string version)
        {
            Hero = hero;
            Version = version;
        }

        public IHero Hero { get; }

        public string Version { get; }
    }

    public class GetHeroResultInfo : IOperationResultDataInfo
    {
        public GetHeroResultInfo(
            EntityId heroId,
            string version,
            IReadOnlyCollection<EntityId> entityIds)
        {
            HeroId = heroId;
            Version = version;
            EntityIds = entityIds;
        }

        public EntityId HeroId { get; }

        public string Version { get; }

        public IReadOnlyCollection<EntityId> EntityIds { get; }
    }

    public interface IOperationResultDataFactory<out TResult>
    {
        TResult Create(IOperationResultDataInfo resultInfo);
    }

    public interface IOperationResultDataInfo
    {
        IReadOnlyCollection<EntityId> EntityIds { get; }
    }
}
