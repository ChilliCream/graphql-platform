using HotChocolate.StarWars.Models;

namespace HotChocolate.StarWars.Data;

public class ReviewRepository
{
    private readonly Dictionary<Episode, List<Review>> _data =
        new Dictionary<Episode, List<Review>>();

    public void AddReview(Episode episode, Review review)
    {
        if (!_data.TryGetValue(episode, out var reviews))
        {
            reviews = [];
            _data[episode] = reviews;
        }

        reviews.Add(review);
    }

    public IEnumerable<Review> GetReviews(Episode episode)
    {
        if (_data.TryGetValue(episode, out var reviews))
        {
            return reviews;
        }
        return Array.Empty<Review>();
    }
}
