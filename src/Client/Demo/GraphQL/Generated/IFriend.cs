using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace Foo
{
    public interface IFriend
    {
        IReadOnlyList<IHasName> Nodes { get; }
    }
}
