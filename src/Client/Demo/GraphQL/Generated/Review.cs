using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
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
