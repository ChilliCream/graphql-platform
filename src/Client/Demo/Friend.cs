using System;
using System.Collections.Generic;
using StrawberryShake;

namespace Foo
{
    public class Friend
        : IFriend
    {
        public IReadOnlyList<IHasName> Nodes { get; set; }
    }}
