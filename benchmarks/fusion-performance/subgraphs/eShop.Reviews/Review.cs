namespace eShop.Reviews;

public sealed class Review
{
    public required string Id { get; init; }

    public required string Body { get; init; }

    public required string AuthorId { get; init; }

    public required string ProductUpc { get; init; }
}
