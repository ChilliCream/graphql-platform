using System;
using System.Collections.Generic;
using ChilliCream.Testing;

namespace StrawberryShake.Integration.Mappers
{
    public class DroidHeroMapper : IEntityMapper<DroidEntity, DroidHero>
    {
        private readonly IEntityStore _entityStore;
        private readonly IEntityMapper<HumanEntity, Human> _humanMapper;
        private readonly IEntityMapper<DroidEntity, Droid> _droidMapper;

        public DroidHeroMapper(
            IEntityStore entityStore,
            IEntityMapper<HumanEntity, Human> humanMapper,
            IEntityMapper<DroidEntity, Droid> droidMapper)
        {
            _entityStore = entityStore ?? throw new ArgumentNullException(nameof(entityStore));
            _humanMapper = humanMapper ?? throw new ArgumentNullException(nameof(humanMapper));
            _droidMapper = droidMapper ?? throw new ArgumentNullException(nameof(droidMapper));
        }

        public DroidHero Map(DroidEntity entity)
        {
            var friends = new List<ICharacter>();
            IEntityStoreSnapshot snapshot = _entityStore.CurrentSnapshot;

            foreach (EntityId friendId in entity.Friends)
            {
                if (friendId.Name.Equals("Human", StringComparison.Ordinal))
                {
                    friends.Add(
                        _humanMapper.Map(
                            snapshot.GetEntity<HumanEntity>(friendId)!));
                }
                else if (friendId.Name.Equals("Droid", StringComparison.Ordinal))
                {
                    friends.Add(
                        _droidMapper.Map(
                            snapshot.GetEntity<DroidEntity>(friendId)!));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            // Note:
            // The friends connection data with the totalCount would be
            // on the entity. We still have to think about paging data here and
            // how to handle it.
            return new DroidHero(entity.Name, new FriendsConnection(friends, 1000));
        }
    }
}
