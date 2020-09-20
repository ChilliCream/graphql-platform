using System;
using System.Diagnostics.Tracing;
using HotChocolate.Subscriptions;
using HotChocolate.StarWars.Data;
using HotChocolate.StarWars.Models;

namespace HotChocolate.StarWars
{
    public class Subscription
    {
        public Review OnReview(
            Episode episode,
            [EventMessage]Review review,
            [Service]ReviewRepository repository)
        {
            return review;
        }
    }
}
