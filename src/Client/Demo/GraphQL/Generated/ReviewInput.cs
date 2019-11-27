using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    public class ReviewInput
    {
        public Optional<string?> Commentary { get; set; }

        public Optional<int> Stars { get; set; }
    }
}
