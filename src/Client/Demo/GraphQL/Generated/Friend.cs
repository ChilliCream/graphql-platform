using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    public class Friend
        : IFriend
    {
        public Friend(
            IReadOnlyList<IHasName>? nodes)
        {
            Nodes = nodes;
        }

        public IReadOnlyList<IHasName>? Nodes { get; }
    }
}
