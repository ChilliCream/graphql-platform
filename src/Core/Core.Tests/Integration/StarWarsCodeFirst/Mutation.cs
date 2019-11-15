using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Subscriptions;

namespace HotChocolate.Integration.StarWarsCodeFirst
{
    public class Mutation
    {
        public async Task<Review> CreateReview(
            Episode episode,
            Review review,
            [Service]IEventSender eventSender)
        {
            await eventSender.SendAsync(
                new OnCreateReviewMessage(episode, review));

            return review;
        }
    }
}
