using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.StarWarsQuery
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class Friend
        : IFriend
    {
        public Friend(
            global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.Client.StarWarsQuery.IHasName>? nodes)
        {
            Nodes = nodes;
        }

        public global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.Client.StarWarsQuery.IHasName>? Nodes { get; }
    }
}
