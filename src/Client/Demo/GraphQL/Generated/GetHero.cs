using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "0.0.0.0")]
    public class GetHero
        : IGetHero
    {
        public GetHero(
            IHasName? hero)
        {
            Hero = hero;
        }

        public IHasName? Hero { get; }
    }
}
