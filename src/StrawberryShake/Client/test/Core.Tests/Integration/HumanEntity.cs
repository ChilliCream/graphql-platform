using System.Collections.Generic;

namespace StrawberryShake.Integration
{
    public class HumanEntity
    {
        public HumanEntity(EntityId id, string name, List<EntityId> friends)
        {
            Id = id;
            Name = name;
            Friends = friends;
        }

        public EntityId Id { get; }

        public string Name { get; }

        public List<EntityId> Friends { get; }
    }
}
