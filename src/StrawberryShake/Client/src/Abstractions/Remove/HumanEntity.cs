using System.Collections.Generic;

namespace StrawberryShake.Remove
{
    public class HumanEntity
    {
        public EntityId Id { get; set; }
        public string Name { get; set; }
        public List<EntityId> Friends { get; set; }
    }
}
