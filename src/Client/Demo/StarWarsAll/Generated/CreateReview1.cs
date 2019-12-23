using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.StarWarsAll
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public class CreateReview1
        : ICreateReview
    {
        public CreateReview1(
            IReview createReview)
        {
            CreateReview = createReview;
        }

        public IReview CreateReview { get; }
    }
}
