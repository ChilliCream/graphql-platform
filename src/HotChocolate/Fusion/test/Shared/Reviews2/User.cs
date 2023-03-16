using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Reviews2;

[Node]
public sealed class User : IReviewOrAuthor
{
    public User(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; }

    public string Name { get; }

    public IEnumerable<Review> GetReviews([Service] ReviewRepository repository)
        => repository.GetReviewsByAuthorId(Id);
}
