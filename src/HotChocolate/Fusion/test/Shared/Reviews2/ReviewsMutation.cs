namespace HotChocolate.Fusion.Shared.Reviews2;

public sealed class ReviewsMutation
{
    public Review AddReview(
        [Service] ReviewRepository repository,
        string body,
        int authorId,
        int upc)
        => new Review(
            repository.GetReviews().OrderByDescending(t => t.Id).Last().Id,
            repository.GetAuthor(authorId)!,
            new Product(upc),
            body);
}
