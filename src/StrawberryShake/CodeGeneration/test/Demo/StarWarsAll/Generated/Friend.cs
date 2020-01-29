using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.StarWarsAll
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
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
