using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "0.0.0.0")]
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
