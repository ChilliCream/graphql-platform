using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Reviews2;

[Node]
public sealed record Review(int Id, User Author, Product Product, string Body) : IReviewOrAuthor;
