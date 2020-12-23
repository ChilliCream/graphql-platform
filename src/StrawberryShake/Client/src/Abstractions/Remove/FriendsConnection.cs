using System.Collections.Generic;

namespace StrawberryShake.Remove
{
    public class FriendsConnection
    {
        public FriendsConnection(IReadOnlyList<ICharacter> nodes, int totalCount)
        {
            Nodes = nodes;
            TotalCount = totalCount;
        }

        public IReadOnlyList<ICharacter> Nodes { get; }

        public int TotalCount { get; }
    }
}
