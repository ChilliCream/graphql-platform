using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client
{
    public class GetHero
        : IGetHero
    {
        public IHero Hero { get; set; }
    }
}
