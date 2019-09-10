using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client
{
    public class Friend
        : IFriend
    {
        public IReadOnlyList<IHasName> Nodes { get; set; }
    }
}
