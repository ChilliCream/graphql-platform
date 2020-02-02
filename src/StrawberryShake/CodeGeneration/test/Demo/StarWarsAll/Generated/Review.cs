using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.StarWarsAll
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public class Review
        : IReview
    {
        public Review(
            string? commentary)
        {
            Commentary = commentary;
        }

        public string? Commentary { get; }
    }
}
