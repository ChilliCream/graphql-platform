using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Reviews;

[Node]
public sealed class Author
{
    public Author(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; }

    public string Name { get; }

    public IEnumerable<Review> GetReviews([Service] ReviewRepository repository)
        => repository.GetReviewsByAuthorId(Id);
}
