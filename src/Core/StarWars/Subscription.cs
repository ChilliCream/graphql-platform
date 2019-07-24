using System;
using HotChocolate.Subscriptions;
using HotChocolate.StarWars.Data;
using HotChocolate.StarWars.Models;

namespace HotChocolate.StarWars
{
    public class Subscription
    {
        private readonly ReviewRepository _repository;

        public Subscription(ReviewRepository repository)
        {
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
        }

        public Review OnReview(Episode episode, IEventMessage message)
        {
            return (Review)message.Payload;
        }
    }
}
