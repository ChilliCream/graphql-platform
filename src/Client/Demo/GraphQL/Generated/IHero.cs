using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace Foo
{
    public interface IHero
        : IHasName
        , IHasFriends
    {
        double? Height { get; }
    }
}
