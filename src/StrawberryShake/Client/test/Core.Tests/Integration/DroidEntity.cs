using System.Collections.Generic;

namespace StrawberryShake.Integration
{
    public class DroidEntity
    {
        public EntityId Id { get; set; }
        public string Name { get; set; }
        public List<EntityId> Friends { get; set; }
    }
}
