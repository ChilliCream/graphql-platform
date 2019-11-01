using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    public class GetHuman
        : IGetHuman
    {
        public GetHuman(
            IHero? human)
        {
            Human = human;
        }

        public IHero? Human { get; }
    }
}
