using System;
using System.Collections.Generic;
using StarWars.Models;

namespace StarWars.Data
{
    public class ReviewRepository
    {
        private readonly Dictionary<Episode, List<Review>> _data =
            new Dictionary<Episode, List<Review>>();

        public void AddReview(Episode episode, Review review)
        {
            if (!_data.TryGetValue(episode, out List<Review> reviews))
            {
                reviews = new List<Review>();
                _data[episode] = reviews;
            }

            reviews.Add(review);
        }

        public IEnumerable<Review> GetReviews(Episode episode)
        {
            if (_data.TryGetValue(episode, out List<Review> reviews))
            {
                return reviews;
            }
            return Array.Empty<Review>();
        }
    }
}
