using System.Collections.Generic;

namespace StrawberryShake.Remove
{
    public class GetHeroResultInfo : IOperationResultDataInfo
    {
        private readonly IReadOnlyCollection<EntityId> _entityIds;
        private readonly ulong _version;

        public GetHeroResultInfo(
            EntityId heroId,
            string version,
            IReadOnlyCollection<EntityId> entityIds,
            ulong entityVersion)
        {
            HeroId = heroId;
            Version = version;
            _entityIds = entityIds;
            _version = entityVersion;
        }

        public EntityId HeroId { get; }

        public string Version { get; }


        IReadOnlyCollection<EntityId> IOperationResultDataInfo.EntityIds => _entityIds;

        ulong IOperationResultDataInfo.Version => _version;

        IOperationResultDataInfo IOperationResultDataInfo.WithVersion(ulong version)
        {
            return new GetHeroResultInfo(HeroId, Version, _entityIds, version);
        }
    }
}
