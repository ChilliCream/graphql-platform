using HotChocolate.Subscriptions;
using HotChocolate.StarWars.Data;
using HotChocolate.StarWars.Models;
using static HotChocolate.StarWars.Types.Subscriptions;

namespace HotChocolate.StarWars;

public class Mutation
{
    /// <summary>
    /// Creates a review for a given Star Wars episode.
    /// </summary>
    /// <param name="episode">The episode to review.</param>
    /// <param name="review">The review.</param>
    /// <param name="repository">The review repository.</param>
    /// <param name="eventSender">The event sending service.</param>
    /// <returns>The created review.</returns>
    public async Task<Review> CreateReview(
        Episode episode,
        Review review,
        [Service] ReviewRepository repository,
        [Service] ITopicEventSender eventSender)
    {
        repository.AddReview(episode, review);
        await eventSender.SendAsync($"{OnReview}_{episode}", review);
        return review;
    }

    public async Task<bool> CompleteAsync(
        Episode episode,
        [Service] ITopicEventSender eventSender)
    {
        await eventSender.CompleteAsync($"{OnReview}_{episode}");
        return true;
    }
}
