using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "0.0.0.0")]
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
