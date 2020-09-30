using HotChocolate;
using HotChocolate.ApolloFederation;
using Reviews.Data;
using Review = Reviews.Models.Review;

namespace Reviews
{
    public class Query
    {
        // This resolver is not in the official ApolloFederationDemo, but is
        // needed to create a valid schemema
        public Review GetReviewById(
            [Service] ReviewRepository reviewRepository,
            string id)
        {
            return reviewRepository.GetById(id);
        }
    }
}
