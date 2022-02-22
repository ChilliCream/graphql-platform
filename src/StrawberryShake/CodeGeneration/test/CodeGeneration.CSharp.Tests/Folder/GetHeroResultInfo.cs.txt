using System;
using System.Collections.Generic;
using StrawberryShake;

namespace Foo.Bar.State;

[global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
public partial class GetHeroResultInfo : IOperationResultDataInfo
{
    public GetHeroResultInfo(
        EntityId? hero,
        IReadOnlyCollection<EntityId> entityIds,
        IReadOnlyDictionary<string, EntityId> pathToEntityId,
        ulong version)
    {
        Hero = hero;
        EntityIds = entityIds ?? throw new ArgumentNullException(nameof(entityIds));
        PathToEntityId = pathToEntityId ?? throw new ArgumentNullException(nameof(pathToEntityId));
        Version = version;
    }

    public EntityId? Hero { get; }

    public IReadOnlyCollection<EntityId> EntityIds { get; }

    public IReadOnlyDictionary<string, EntityId> PathToEntityId { get; }

    public ulong Version { get; }

    public IOperationResultDataInfo WithVersion(ulong version)
        => new GetHeroResultInfo(Hero, EntityIds, PathToEntityId, version);
}
