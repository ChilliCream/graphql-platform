namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Reviews;

/// <summary>
/// The <c>Review</c> entity in the <c>reviews</c> subgraph
/// (<c>@key(fields: "id")</c>). Carries the foreign keys
/// <see cref="AuthorId"/> and <see cref="ProductUpc"/> so the resolvers for
/// <see cref="ReviewType.Configure"/> can project the related <c>User</c>
/// and <c>Product</c> entities.
/// </summary>
public sealed class Review
{
    public string Id { get; init; } = default!;

    public string? Body { get; init; }

    public string? AuthorId { get; init; }

    public string? ProductUpc { get; init; }
}
