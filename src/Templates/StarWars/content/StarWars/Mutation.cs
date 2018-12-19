using System;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Subscriptions;
using StarWars.Data;
using StarWars.Models;

namespace StarWars
{
    public class Mutation
    {
        private readonly ReviewRepository _repository;

        public Mutation(ReviewRepository repository)
        {
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Review> CreateReview(
            Episode episode, Review review,
            [Service]IEventSender eventSender)
        {
            _repository.AddReview(episode, review);
            await eventSender.SendAsync(new OnReviewMessage(episode, review));
            return review;
        }
    }
}
