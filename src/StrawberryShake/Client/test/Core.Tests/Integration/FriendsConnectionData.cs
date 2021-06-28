using System.Collections.Generic;

namespace StrawberryShake.Integration
{
    public class FriendsConnectionData
    {
        public FriendsConnectionData(IReadOnlyList<EntityId> nodes, int totalCount)
        {
            Nodes = nodes;
            TotalCount = totalCount;
        }

        public IReadOnlyList<EntityId> Nodes { get; }

        public int TotalCount { get; }
    }
}
