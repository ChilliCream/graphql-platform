namespace Reviews;

public class Query
{
    // This resolver is not in the official ApolloFederationDemo, but is
    // needed to create a valid schema
    public Review GetReviewById(
        ReviewRepository reviewRepository,
        string id)
        => reviewRepository.GetById(id);
}
