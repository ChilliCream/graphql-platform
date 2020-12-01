using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.StarWarsAll
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public class OnReview1
        : IOnReview
    {
        public OnReview1(
            IReview onReview)
        {
            OnReview = onReview;
        }

        public IReview OnReview { get; }
    }
}
