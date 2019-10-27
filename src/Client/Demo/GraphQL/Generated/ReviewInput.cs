using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace  StrawberryShake.Client.GraphQL
{
    public class ReviewInput
    {
        public string? Commentary { get; set; }

        public int Stars { get; set; }
    }
}
