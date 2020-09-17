﻿using HotChocolate;
using HotChocolate.Types;
using StarWars.Characters;

namespace StarWars.Reviews
{
    [ExtendObjectType(Name = "Subscription")]
    public class ReviewSubscriptions
    {
        [Subscribe]
        public Review OnReview(
            [Topic]Episode episode, 
            [EventMessage]Review message) => 
            message;
    }
}
