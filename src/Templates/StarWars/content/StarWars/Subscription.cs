using System;
using HotChocolate.Subscriptions;
using StarWars.Data;
using StarWars.Models;

namespace StarWars
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
