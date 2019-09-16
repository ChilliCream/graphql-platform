using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client
{
    public class Friend0
        : IFriend0
    {
        public Friend0(
            IReadOnlyList<IHasName> nodes)
        {
            nodes = Nodes;
        }
        public IReadOnlyList<IHasName> Nodes { get; }
    }
}
