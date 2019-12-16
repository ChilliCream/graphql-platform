using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.StarWarsAll
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
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
