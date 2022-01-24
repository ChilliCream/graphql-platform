using HotChocolate.ApolloFederation;

namespace Reviews;

[ExtendServiceType]
public class User
{
    [Key]
    [External]
    public string Id { get; set; } = default!;

    [External]
    public string Username { get; set; } = default!;

    public IEnumerable<Review> GetReviews(ReviewRepository reviewRepository)
        => reviewRepository.GetByUserId(Id);
}
