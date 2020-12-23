using System.Collections.Generic;

namespace StrawberryShake.Remove
{
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
}
