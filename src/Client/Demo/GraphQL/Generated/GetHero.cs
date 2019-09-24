using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client
{
    public class GetHero
        : IGetHero
    {
        public GetHero(
            IHero hero)
        {
            Hero = hero;
        }

        public IHero Hero { get; }
    }
}
