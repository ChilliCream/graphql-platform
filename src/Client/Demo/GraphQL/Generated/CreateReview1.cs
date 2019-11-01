using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
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
