using System.Collections.Generic;

namespace StrawberryShake
{
    public class FooEntity
    {
        public string Id { get; set; }
        public string Bar { get; set; }
        public EntityId Baz { get; set; }
        public List<EntityId> Bars { get; set; }
    }
}
