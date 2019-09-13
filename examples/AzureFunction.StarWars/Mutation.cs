using System;
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

        /// <summary>
        /// Creates a review for a given Star Wars episode.
        /// </summary>
        /// <param name="episode">The episode to review.</param>
        /// <param name="review">The review.</param>
        /// <param name="eventSender">The event sending service.</param>
        /// <returns>The created review.</returns>
        public Review CreateReview(
            Episode episode, Review review)
        {
            _repository.AddReview(episode, review);
            return review;
        }
    }
}
