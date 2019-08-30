using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace Foo
{
    public class GetHero
        : IGetHero
    {
        public IHero Hero { get; set; }
    }
}
